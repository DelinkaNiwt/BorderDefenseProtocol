using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger.Shield
{
    /// <summary>
    /// 护盾Hediff核心逻辑组件
    /// 实现护盾的所有功能：方向判定、成功率判定、Trion消耗、特效播放
    /// </summary>
    [StaticConstructorOnStartup]
    public class HediffComp_BDPShield : HediffComp
    {
        // ==================== 静态资源 ====================

        /// <summary>
        /// 护盾球体材质（使用原版护盾材质）
        /// </summary>
        private static readonly Material BubbleMat = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent);

        // ==================== 属性 ====================

        /// <summary>
        /// 配置属性（快捷访问）
        /// </summary>
        public HediffCompProperties_BDPShield Props => (HediffCompProperties_BDPShield)props;

        /// <summary>
        /// 护盾是否激活
        /// 条件：pawn存在、已生成、未死亡、未倒地、且护盾功能启用
        /// </summary>
        private bool IsShieldActive
        {
            get
            {
                if (Pawn == null || !Pawn.Spawned || Pawn.Dead || Pawn.Downed)
                    return false;

                // 检查当前配置的护盾成功率
                // 如果Severity>=2且stackedBlockChance=0，说明护盾被禁用（如重刃模式）
                float currentBlockChance = parent.Severity >= 2f ? Props.stackedBlockChance : Props.blockChance;
                return currentBlockChance > 0f;
            }
        }

        // ==================== 绘制 ====================

        /// <summary>
        /// 绘制护盾球体（由Harmony patch调用）
        /// </summary>
        public void DrawShieldBubble(Vector3 drawLoc)
        {
            // 检查是否应该显示
            if (!IsShieldActive || !Props.showShieldBubble) return;

            // 计算绘制位置（高度在Mote层）
            Vector3 drawPos = drawLoc;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            // 护盾大小
            float size = Props.shieldRadius * 2f; // 直径

            // 随机旋转角度（让护盾看起来有动态感）
            float angle = Rand.Range(0, 360);

            // 设置缩放
            Vector3 scale = new Vector3(size, 1f, size);

            // 创建变换矩阵
            Matrix4x4 matrix = default;
            matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), scale);

            // 绘制护盾球体
            Graphics.DrawMesh(MeshPool.plane10, matrix, BubbleMat, 0);
        }

        // ==================== RimWorld钩子 ====================

        /// <summary>
        /// 尝试抵挡伤害（由Harmony patch调用）
        /// </summary>
        /// <param name="dinfo">伤害信息（可修改）</param>
        /// <returns>true=伤害被完全吸收，false=伤害未被吸收</returns>
        public bool TryBlockDamage(ref DamageInfo dinfo)
        {
            // 1. 护盾激活检查
            if (!IsShieldActive)
            {
                Log.Message($"[BDP-Shield] {Pawn.LabelShort} 护盾未激活");
                return false;
            }

            // 2. 伤害类型检查
            if (!Props.CanAbsorb(dinfo.Def))
            {
                Log.Message($"[BDP-Shield] {Pawn.LabelShort} 伤害类型不可吸收: {dinfo.Def?.defName}");
                return false;
            }

            // 3. 判断是否是近战伤害（用于后续应用不同的倍率）
            bool isMelee = dinfo.Def.hasForcefulImpact && !dinfo.Def.isRanged;

            // 4. 方向判定
            if (!CheckAngle(dinfo.Angle))
            {
                Log.Message($"[BDP-Shield] {Pawn.LabelShort} 方向判定失败");
                return false;
            }

            // 5. 成功率判定（传入是否近战）
            if (!CheckBlockChance(isMelee))
            {
                float actualChance = GetActualBlockChance(isMelee);
                Log.Warning($"[BDP-Shield] {Pawn.LabelShort} 成功率判定失败（{actualChance * 100:F1}%成功率，{(isMelee ? "近战" : "远程")}）");
                return false;
            }

            // 6. Trion消耗（传入是否近战）
            if (!ConsumeTrion(dinfo.Amount, isMelee))
            {
                // Trion不足，护盾失效
                Log.Warning($"[BDP-Shield] {Pawn.LabelShort} Trion不足，护盾失效");
                Break();
                return false;
            }

            // 7. 吸收伤害
            dinfo.SetAmount(0f);

            // 8. 播放特效
            PlayBlockEffect(dinfo);

            Log.Message($"[BDP-Shield] {Pawn.LabelShort} 护盾成功抵挡{(isMelee ? "近战" : "远程")}攻击！");
            return true;
        }

        // ==================== 判定逻辑 ====================

        /// <summary>
        /// 方向判定：检查攻击是否来自护盾可抵挡的角度范围
        /// v2.0：根据parent.Severity动态选择配置参数
        /// </summary>
        /// <param name="damageAngle">伤害角度（子弹飞行方向，0-360度）</param>
        /// <returns>true=在范围内，false=不在范围内</returns>
        private bool CheckAngle(float damageAngle)
        {
            float severity = parent.Severity;

            // 根据Severity选择配置
            bool enableCheck;
            float angleRange;
            float angleOffset = Props.blockAngleOffset; // 偏移量不变

            if (severity >= 2f)
            {
                // Severity>=2：使用叠加配置
                enableCheck = Props.stackedEnableAngleCheck;
                angleRange = Props.stackedBlockAngleRange;
            }
            else
            {
                // Severity=1：使用基础配置
                enableCheck = Props.enableAngleCheck;
                angleRange = Props.blockAngleRange;
            }

            // 如果未启用角度检查，全方位防护
            if (!enableCheck)
            {
                Log.Message($"[BDP-Shield] {Pawn.LabelShort} 全方位护盾（Severity={severity}），跳过角度检查");
                return true;
            }

            // 获取pawn朝向（度数）
            float pawnRotation = Pawn.Rotation.AsAngle;

            // 计算攻击来源方向（子弹飞行方向的反向）
            // 例如：子弹从南向北飞（0°），攻击来源在南（180°）
            float attackSourceAngle = (damageAngle + 180f) % 360f;

            // 计算攻击来源相对于pawn朝向的角度（-180到180度）
            // 例如：pawn朝南（180°），攻击来源在南（180°），相对角度=0°（正面）
            float relativeAngle = Mathf.DeltaAngle(pawnRotation, attackSourceAngle);

            // 计算允许的角度范围
            float minAngle = angleOffset - angleRange / 2f;
            float maxAngle = angleOffset + angleRange / 2f;

            // 调试日志
            bool inRange = relativeAngle >= minAngle && relativeAngle <= maxAngle;
            Log.Message($"[BDP-Shield] {Pawn.LabelShort} 护盾角度判定（Severity={severity}）: " +
                       $"pawn朝向={pawnRotation:F1}°, 子弹方向={damageAngle:F1}°, " +
                       $"攻击来源={attackSourceAngle:F1}°, 相对角度={relativeAngle:F1}°, " +
                       $"范围=[{minAngle:F1}°, {maxAngle:F1}°], 结果={inRange}");

            // 判断是否在范围内
            return inRange;
        }

        /// <summary>
        /// 成功率判定：随机检查是否成功抵挡
        /// v2.0：根据parent.Severity动态选择成功率
        /// v3.0：支持近战成功率倍率
        /// </summary>
        /// <param name="isMelee">是否是近战伤害</param>
        /// <returns>true=成功抵挡，false=抵挡失败</returns>
        private bool CheckBlockChance(bool isMelee = false)
        {
            float actualChance = GetActualBlockChance(isMelee);

            // 100%成功率，直接返回true
            if (actualChance >= 1f) return true;

            // 随机判定
            bool success = Rand.Value < actualChance;
            Log.Message($"[BDP-Shield] {Pawn.LabelShort} 成功率判定（Severity={parent.Severity}, {(isMelee ? "近战" : "远程")}）: " +
                       $"成功率={actualChance * 100:F1}%, 结果={success}");
            return success;
        }

        /// <summary>
        /// 获取实际成功率（考虑Severity和近战倍率）
        /// </summary>
        /// <param name="isMelee">是否是近战伤害</param>
        /// <returns>实际成功率</returns>
        private float GetActualBlockChance(bool isMelee)
        {
            float severity = parent.Severity;

            // 根据Severity选择基础成功率
            float baseChance = (severity >= 2f) ? Props.stackedBlockChance : Props.blockChance;

            // 如果是近战，应用近战倍率
            if (isMelee)
            {
                baseChance *= Props.meleeBlockChanceMultiplier;
            }

            return baseChance;
        }

        // ==================== Trion系统 ====================

        /// <summary>
        /// 消耗Trion
        /// v3.0：支持近战Trion消耗倍率
        /// </summary>
        /// <param name="damageAmount">伤害值</param>
        /// <param name="isMelee">是否是近战伤害</param>
        /// <returns>true=消耗成功，false=Trion不足</returns>
        private bool ConsumeTrion(float damageAmount, bool isMelee = false)
        {
            // 获取Trion组件
            var trionComp = Pawn.GetComp<CompTrion>();
            if (trionComp == null) return false;

            // 计算Trion消耗：伤害值 × 基础倍率 × 近战倍率（如果是近战）
            float trionCost = damageAmount * Props.trionCostMultiplier;
            if (isMelee)
            {
                trionCost *= Props.meleeTrionCostMultiplier;
            }

            // 从可用量（Available）中消耗
            return trionComp.Consume(trionCost);
        }

        // ==================== 特效和反馈 ====================

        /// <summary>
        /// 播放护盾抵挡特效
        /// </summary>
        /// <param name="dinfo">伤害信息</param>
        private void PlayBlockEffect(DamageInfo dinfo)
        {
            // 计算特效位置：在护盾边缘（子弹被拦截的位置）
            // dinfo.Angle 是伤害角度（子弹飞行方向）
            // 子弹来的方向是反向，所以需要旋转180度

            Vector3 pawnCenter = Pawn.TrueCenter();
            Vector3 direction = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle + 180f);
            Vector3 impactPos = pawnCenter + direction * Props.shieldRadius;

            // 1. 播放护盾特效（自定义的护盾视觉效果）
            ShieldEffectPlayer.PlayBlockEffect(impactPos, Pawn.Map, Props.blockEffectDef, Props.effectScale);

            // 2. 播放金属抵挡特效（原版的火花效果）
            EffecterDef deflectEffect = EffecterDefOf.Deflect_Metal_Bullet;
            if (deflectEffect != null)
            {
                Effecter effecter = deflectEffect.Spawn();
                effecter.Trigger(new TargetInfo(impactPos.ToIntVec3(), Pawn.Map), TargetInfo.Invalid);
                effecter.Cleanup();
            }
        }

        /// <summary>
        /// 护盾失效（Trion不足时调用）
        /// </summary>
        private void Break()
        {
            // 当前实现：静默失效，等待Trion恢复
            // 未来可扩展：
            // - 播放护盾破碎特效
            // - 播放音效
            // - 发送消息通知玩家
        }
    }
}

