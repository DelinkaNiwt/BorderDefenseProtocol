using BDP.Core;
using BDP.Projectiles;
using BDP.Trigger.ShotPipeline;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 组合技攻击Verb（v10.0新增，v12.0重命名）——B+C芯片组合发射天眼弹。
    ///
    /// 继承Verb_BDPRangedBase，复用引导弹基础设施（gs、StartAnchorTargeting等）。
    /// 参数取两侧芯片的平均值（range、warmup、burst、trionCost、anchorSpread）。
    ///
    /// 内部通过firingPattern标志区分普通/齐射模式，避免新增子类：
    ///   · Sequential：引擎burst机制逐发射击
    ///   · Simultaneous：burstShotCount=1，TryCastShot内循环发射
    /// </summary>
    public class Verb_BDPCombo : Verb_BDPRangedBase
    {
        /// <summary>组合技定义引用（由CompTriggerBody.CreateComboVerb设置）。</summary>
        public ComboVerbDef comboDef;

        /// <summary>发射模式（由CompTriggerBody在创建时设置）。</summary>
        internal FiringPattern firingPattern;

        /// <summary>缓存的平均burstShotCount（由Initialize计算）。</summary>
        public int avgBurstCount;

        /// <summary>缓存的平均trionCostPerShot。</summary>
        public float avgTrionCost;

        /// <summary>缓存的平均anchorSpread。</summary>
        public float avgAnchorSpread;

        /// <summary>缓存的平均volleySpreadRadius。</summary>
        public float avgVolleySpread;

        // ── 引导弹支持 ──

        /// <summary>组合技是否支持引导瞄准。</summary>
        public override bool SupportsGuided => comboDef?.supportsGuided == true;

        /// <summary>启动引导瞄准（锚点折线弹道）。</summary>
        public override void StartAnchorTargeting()
        {
            if (comboDef == null || !comboDef.supportsGuided)
            {
                Find.Targeter.BeginTargeting(this);
                return;
            }
            AnchorTargetingHelper.BeginAnchorTargeting(
                this, CasterPawn, comboDef.maxAnchors, verbProps.range,
                (anchors, finalTarget) =>
                {
                    // 将锚点数据存储到 activeSession
                    if (activeSession != null)
                    {
                        activeSession.AnchorPath = new System.Collections.Generic.List<IntVec3>(anchors);
                        if (activeSession.AimResult == null)
                            activeSession.AimResult = new ShotPipeline.AimResult();
                        activeSession.AimResult.AnchorPath = activeSession.AnchorPath;
                        activeSession.AimResult.FinalTarget = finalTarget;
                        activeSession.AimResult.AnchorSpread = avgAnchorSpread;
                    }
                    OrderForceTargetCore(finalTarget);
                });
        }

        /// <summary>弹道发射后回调：通过管线系统注入射击数据。</summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (!(proj is Bullet_BDP bdp)) return;
            if (activeSession == null) return;

            // 从管线系统注入射击数据
            bdp.InjectShotData(
                activeSession.AimResult,
                activeSession.FireResult,
                activeSession.RouteResult);
        }

        // ── 射击逻辑 ──

        /// <summary>
        /// ExecuteFire override：组合技调度逻辑。
        /// 计算两侧芯片的平均参数（burst × FireMode倍率），根据firingPattern分发到齐射/逐发模式。
        /// </summary>
        protected override bool ExecuteFire(ShotSession session)
        {
            var pawn = CasterPawn;
            if (pawn == null || comboDef == null) return false;

            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return false;

            // 获取两侧芯片
            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
            if (leftSlot?.loadedChip == null || rightSlot?.loadedChip == null) return false;

            // 计算有效连射数（基础平均值 × FireMode平均倍率）
            int effectiveBurst = ComputeEffectiveBurst(leftSlot.loadedChip, rightSlot.loadedChip);

            // 调试日志：记录组合技攻击信息（仅在burst开始时记录一次）
            if (burstShotsLeft == verbProps.burstShotCount)
            {
                // 齐射模式：开枪1次，发射effectiveBurst颗子弹
                // 逐发模式：开枪effectiveBurst次，发射effectiveBurst颗子弹
                int shotCount = verbProps.burstShotCount;
                int bulletCount = effectiveBurst;

                // v15.0：根据verbProps.isPrimary判断主/副攻击
                bool isSecondary = !verbProps.isPrimary;
                VerbAttackLogger.LogComboAttack(
                    this,
                    comboDef.defName,
                    new[] { leftSlot.loadedChip.def.defName, rightSlot.loadedChip.def.defName },
                    firingPattern,
                    shotCount,
                    bulletCount,
                    isSecondary
                );
            }

            // 齐射模式：单次ExecuteFire内循环发射
            if (firingPattern == FiringPattern.Simultaneous)
                return DoSimultaneousShot(pawn, triggerComp, leftSlot, rightSlot, effectiveBurst);

            // 普通模式：引擎burst机制，逐发射击
            return DoSequentialShot(pawn, triggerComp, leftSlot, rightSlot, effectiveBurst);
        }

        /// <summary>齐射模式：单次TryCastShot内循环发射所有子弹。</summary>
        private bool DoSimultaneousShot(Pawn pawn, CompTriggerBody tc,
            ChipSlot leftSlot, ChipSlot rightSlot, int volleyCount)
        {
            // 预检Trion总消耗
            float totalCost = volleyCount * avgTrionCost;
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost))
                return false;

            // 选择一侧芯片作为equipmentSource（战斗日志用）
            Thing chipEquipment = leftSlot.loadedChip;

            bool anyHit = FireVolleyLoop(volleyCount, avgVolleySpread, chipEquipment);

            if (anyHit && totalCost > 0f)
                trion?.Consume(totalCost);

            return anyHit;
        }

        /// <summary>普通模式：引擎burst逐发射击，每发检查Trion。</summary>
        private bool DoSequentialShot(Pawn pawn, CompTriggerBody tc,
            ChipSlot leftSlot, ChipSlot rightSlot, int effectiveBurst)
        {
            // FireMode连射截断（burst机制截断法）
            int fired = verbProps.burstShotCount - burstShotsLeft;
            if (fired >= effectiveBurst) { burstShotsLeft = 0; return false; }

            // 单发Trion检查
            if (avgTrionCost > 0f)
            {
                var trion = pawn.GetComp<CompTrion>();
                if (trion == null || trion.Available < avgTrionCost)
                    return false;
            }

            Thing chipEquipment = leftSlot.loadedChip;
            bool result = TryCastShotCore(chipEquipment);

            if (result && avgTrionCost > 0f)
                pawn.GetComp<CompTrion>()?.Consume(avgTrionCost);

            return result;
        }

        // ── 参数计算 ──

        /// <summary>
        /// 计算有效连射数：Round(avgBurst × avg(B.fm.burst, C.fm.burst))。
        /// FireMode为null时倍率视为1.0。
        /// </summary>
        private int ComputeEffectiveBurst(Thing chipA, Thing chipB)
        {
            var fmA = GetFireMode(chipA);
            var fmB = GetFireMode(chipB);
            float burstMultA = fmA?.Burst ?? 1f;
            float burstMultB = fmB?.Burst ?? 1f;
            float avgMult = (burstMultA + burstMultB) * 0.5f;
            return Mathf.Max(1, Mathf.RoundToInt(avgBurstCount * avgMult));
        }
    }
}
