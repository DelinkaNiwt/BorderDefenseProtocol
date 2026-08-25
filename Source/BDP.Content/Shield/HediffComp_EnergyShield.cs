using BDP.Core.Trion;
using BDP.Core.Trigger;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>
    /// 正式能量护盾的运行时 HediffComp。
    /// 负责伤害判定、Trion 扣费和护盾表现，不持有 Trigger 或表达真值。
    /// </summary>
    [StaticConstructorOnStartup]
    public sealed class HediffComp_EnergyShield : HediffComp
    {
        /// <summary>
        /// 复用 RimWorld 原版护盾球材质。
        /// </summary>
        private static readonly Material BubbleMaterial =
            MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent);

        /// <summary>
        /// 当前组件的强类型配置。
        /// </summary>
        public HediffCompProperties_EnergyShield Props
        {
            get { return (HediffCompProperties_EnergyShield)props; }
        }

        /// <summary>
        /// 当前 Pawn 是否满足护盾运行条件。
        /// </summary>
        private bool IsShieldActive
        {
            get
            {
                if (Pawn == null || !Pawn.Spawned || Pawn.Dead || Pawn.Downed)
                {
                    return false;
                }

                return ResolveCurrentBlockChance() > 0f;
            }
        }

        /// <summary>
        /// 尝试按当前配置完全抵挡一次伤害。
        /// </summary>
        public bool TryBlockDamage(ref DamageInfo damageInfo)
        {
            if (!IsShieldActive || !Props.CanAbsorb(damageInfo))
            {
                return false;
            }

            float attackTravelAngle = EnergyShieldBlockPolicy.ResolveAttackTravelAngle(
                Pawn,
                damageInfo);
            if (!CheckAngle(attackTravelAngle) || !CheckBlockChance())
            {
                return false;
            }

            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(Pawn);
            float cost = EnergyShieldBlockPolicy.CalculateTrionCost(
                damageInfo.Amount,
                Props.trionCostMultiplier);
            if (commands == null || !commands.TryConsume(cost))
            {
                return false;
            }

            damageInfo.SetAmount(0f);
            PlayBlockEffect(damageInfo, attackTravelAngle);
            PlayBlockVisualImpulse(attackTravelAngle);
            return true;
        }

        /// <summary>
        /// 在 Pawn 当前绘制位置显示原版护盾球。
        /// </summary>
        public void DrawShieldBubble(Vector3 drawLocation)
        {
            if (!IsShieldActive || !Props.showShieldBubble)
            {
                return;
            }

            Vector3 drawPosition = drawLocation;
            drawPosition.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            float diameter = Props.shieldRadius * 2f;
            Vector3 scale = new Vector3(diameter, 1f, diameter);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(
                drawPosition,
                Quaternion.AngleAxis(Rand.Range(0, 360), Vector3.up),
                scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, BubbleMaterial, 0);
        }

        /// <summary>
        /// 根据当前 Severity 选择方向规则并检查攻击来源。
        /// </summary>
        private bool CheckAngle(float damageAngle)
        {
            bool stacked = parent.Severity >= 2f;
            bool enableCheck = stacked
                ? Props.stackedEnableAngleCheck
                : Props.enableAngleCheck;
            float range = stacked
                ? Props.stackedBlockAngleRange
                : Props.blockAngleRange;

            return EnergyShieldBlockPolicy.IsWithinArc(
                Pawn.Rotation.AsAngle,
                damageAngle,
                enableCheck,
                range,
                Props.blockAngleOffset);
        }

        /// <summary>
        /// 按当前单枚或双枚成功率执行一次原版随机判定。
        /// </summary>
        private bool CheckBlockChance()
        {
            float chance = ResolveCurrentBlockChance();
            return chance >= 1f || Rand.Value < chance;
        }

        /// <summary>
        /// 读取当前 Severity 对应的抵挡成功率。
        /// </summary>
        private float ResolveCurrentBlockChance()
        {
            return EnergyShieldBlockPolicy.ResolveBlockChance(
                parent.Severity,
                Props.blockChance,
                Props.stackedBlockChance);
        }

        /// <summary>
        /// 在攻击来源方向的护盾边缘播放抵挡表现。
        /// </summary>
        private void PlayBlockEffect(DamageInfo damageInfo, float attackTravelAngle)
        {
            Vector3 direction = Vector3Utility.HorizontalVectorFromAngle(attackTravelAngle + 180f);
            Vector3 impactPosition = Pawn.TrueCenter()
                + (direction * Props.ResolveImpactEffectRadius());
            bool renderBehindPawn = Props.useDirectionalImpactDepth
                && EnergyShieldBlockPolicy.ShouldRenderImpactBehindPawn(direction);
            FleckDef blockFlashFleckDef = renderBehindPawn
                ? Props.backgroundBlockFlashFleckDef
                : null;
            EnergyShieldEffectPlayer.Play(
                impactPosition,
                Pawn.Map,
                Props.blockEffectDef,
                Props.effectScale,
                Props.showBlockGraphic,
                blockFlashFleckDef);

            EffecterDef deflectEffect = ResolveDeflectEffect(renderBehindPawn);
            if (deflectEffect == null)
            {
                return;
            }

            TargetInfo source = new TargetInfo(Pawn);
            TargetInfo target = BuildDirectionalEffectTarget(damageInfo, direction);
            Vector3 sourceToTarget = (target.CenterVector3 - source.CenterVector3).normalized;
            float vanillaTargetOffset = deflectEffect.offsetTowardsTarget.min;
            Effecter effecter = deflectEffect.Spawn();
            // 原版偏转特效会再向 B 偏移 0.25 格；在 parent offset 中抵消它，
            // 同时补回 IntVec3 无法表达的盾面小数位置。
            effecter.offset = impactPosition
                - source.CenterVector3
                - (sourceToTarget * vanillaTargetOffset);
            effecter.Trigger(source, target);
            effecter.Cleanup();
        }

        /// <summary>
        /// 按本次前后景结论选择偏转组合特效；未启用或缺少配置时回退原版定义。
        /// </summary>
        private EffecterDef ResolveDeflectEffect(bool renderBehindPawn)
        {
            if (Props.useDirectionalImpactDepth)
            {
                EffecterDef configured = renderBehindPawn
                    ? Props.backgroundDeflectEffectDef
                    : Props.foregroundDeflectEffectDef;
                if (configured != null)
                {
                    return configured;
                }
            }

            return EffecterDefOf.Deflect_Metal_Bullet;
        }

        /// <summary>
        /// 沿攻击来源方向构造原版 Effecter 使用的有效目标。
        /// 取较长向量可降低转成格子后产生的角度误差，目标只参与方向计算，不生成实体。
        /// </summary>
        private TargetInfo BuildDirectionalEffectTarget(
            DamageInfo damageInfo,
            Vector3 sourceDirection)
        {
            Thing instigator = damageInfo.Instigator;
            if (instigator != null && instigator.Spawned && instigator.Map == Pawn.Map)
            {
                return new TargetInfo(instigator);
            }

            IntVec3 targetCell = (Pawn.TrueCenter() + (sourceDirection * 20f)).ToIntVec3();
            return new TargetInfo(targetCell, Pawn.Map);
        }

        /// <summary>
        /// 抵挡成功后通知对应表达视觉播放一次短促内缩与回弹。
        /// </summary>
        private void PlayBlockVisualImpulse(float attackTravelAngle)
        {
            if (Props.blockVisualImpulseTicks <= 0 || Props.blockVisualImpulseDistance <= 0f)
            {
                return;
            }

            Vector3 attackTravelDirection =
                Vector3Utility.HorizontalVectorFromAngle(attackTravelAngle);
            ExpressionVisualFeedbackAccess.NotifyImpact(
                parent,
                attackTravelDirection,
                Props.blockVisualImpulseTicks,
                Props.blockVisualImpulseDistance);
        }
    }
}
