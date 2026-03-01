using BDP.Core;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 组合技攻击Verb（v10.0新增）——B+C芯片组合发射天眼弹。
    ///
    /// 继承Verb_BDPRangedBase，复用引导弹基础设施（gs、StartAnchorTargeting等）。
    /// 参数取两侧芯片的平均值（range、warmup、burst、trionCost、anchorSpread）。
    ///
    /// 内部通过isVolley标志区分普通/齐射模式，避免新增子类：
    ///   · isVolley=false：引擎burst机制逐发射击
    ///   · isVolley=true：burstShotCount=1，TryCastShot内循环发射
    /// </summary>
    public class Verb_BDPComboShoot : Verb_BDPRangedBase
    {
        /// <summary>组合技定义引用（由CompTriggerBody.CreateComboVerb设置）。</summary>
        public ComboVerbDef comboDef;

        /// <summary>是否为齐射模式（true=单次TryCastShot内循环发射所有子弹）。</summary>
        public bool isVolley;

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
                    gs.StoreTargetingResult(anchors, finalTarget, avgAnchorSpread);
                    OrderForceTargetCore(finalTarget);
                });
        }

        /// <summary>重写OrderForceTarget：引导弹时启动锚点瞄准。</summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (CasterPawn == null) return;
            if (SupportsGuided) { StartAnchorTargeting(); return; }
            gs.ManualAnchorsActive = false;
            OrderForceTargetCore(target);
        }

        /// <summary>弹道发射后回调：引导模式走引导路径，否则尝试自动绕行。</summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (gs.ManualAnchorsActive)
                gs.AttachManualFlight(proj);
            else
                gs.AttachAutoRouteFlight(proj, gs.ResolveAutoRouteFinalTarget(currentTarget), avgAnchorSpread);
        }

        // ── 射击逻辑 ──

        protected override bool TryCastShot()
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

            // 齐射模式：单次TryCastShot内循环发射
            if (isVolley)
                return DoVolleyShot(pawn, triggerComp, leftSlot, rightSlot, effectiveBurst);

            // 普通模式：引擎burst机制，逐发射击
            return DoBurstShot(pawn, triggerComp, leftSlot, rightSlot, effectiveBurst);
        }

        /// <summary>齐射模式：单次TryCastShot内循环发射所有子弹。</summary>
        private bool DoVolleyShot(Pawn pawn, CompTriggerBody tc,
            ChipSlot leftSlot, ChipSlot rightSlot, int volleyCount)
        {
            // 预检Trion总消耗
            float totalCost = volleyCount * avgTrionCost;
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost))
                return false;

            // 选择一侧芯片作为equipmentSource（战斗日志用）
            Thing chipEquipment = leftSlot.loadedChip;

            // 自动绕行：齐射前计算路由
            gs.PrepareAutoRoute(caster.Position, currentTarget.Cell,
                caster.Map, comboDef.projectileDef);

            bool anyHit = false;
            for (int i = 0; i < volleyCount; i++)
            {
                if (avgVolleySpread > 0f)
                    shotOriginOffset = new Vector3(
                        Rand.Range(-avgVolleySpread, avgVolleySpread), 0f,
                        Rand.Range(-avgVolleySpread, avgVolleySpread));

                if (TryCastShotCore(chipEquipment))
                    anyHit = true;
            }
            shotOriginOffset = Vector3.zero;

            if (anyHit && totalCost > 0f)
                trion?.Consume(totalCost);

            return anyHit;
        }

        /// <summary>普通模式：引擎burst逐发射击，每发检查Trion。</summary>
        private bool DoBurstShot(Pawn pawn, CompTriggerBody tc,
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

            // 自动绕行（首发时计算）
            if (fired == 0)
                gs.PrepareAutoRoute(caster.Position, currentTarget.Cell,
                    caster.Map, comboDef.projectileDef);

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
