using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 双侧混合Verb（v11.0新增）。
    /// 一侧齐射（瞬发所有子弹），一侧逐发（引擎burst机制）。
    /// 齐射侧在burst中只发射一次，逐发侧按正常burst机制交替发射。
    ///
    /// 发射顺序示例：
    /// - 左齐射(5发) + 右逐发(7发) → L(瞬发5发) -> R -> R -> R -> R -> R -> R -> R
    /// - 左逐发(6发) + 右齐射(4发) → L -> R(瞬发4发) -> L -> L -> L -> L -> L
    /// </summary>
    public class Verb_BDPDualMixed : Verb_BDPDualBase
    {
        private bool leftIsVolley = false;
        private bool rightIsVolley = false;
        private bool leftVolleyFired = false;
        private bool rightVolleyFired = false;

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            dualBurstIndex = 0;
            leftRemaining = 0;
            rightRemaining = 0;
            leftProjectileDef = null;
            rightProjectileDef = null;
            leftVolleyFired = false;
            rightVolleyFired = false;

            gs.ResetAutoRouteCastState();
            LocalTargetInfo actualTarget = gs.InterceptDualCastTarget(ref castTarg, caster.Position, caster.Map);
            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
            if (result) gs.PostDualCastOn(ref currentTarget, actualTarget);
            return result;
        }

        public override void Reset()
        {
            base.Reset();
            leftVolleyFired = false;
            rightVolleyFired = false;
        }

        protected override bool TryCastShot()
        {
            if (dualBurstIndex == 0)
                InitDualBurst();

            SlotSide shotSide = GetCurrentShotSide();
            gs.CurrentShotHasPath = gs.ManualAnchorsActive && IsSideGuided(shotSide);

            var triggerComp = GetTriggerComp();
            var pawn = CasterPawn;
            bool isVolleySide = (shotSide == SlotSide.LeftHand && leftIsVolley)
                             || (shotSide == SlotSide.RightHand && rightIsVolley);

            if (isVolleySide)
            {
                // 齐射侧：瞬发所有子弹
                return FireVolleySide(shotSide, triggerComp, pawn);
            }
            else
            {
                // 逐发侧：发射单发
                return FireShootSide(shotSide, triggerComp, pawn);
            }
        }

        private bool FireVolleySide(SlotSide side, CompTriggerBody triggerComp, Pawn pawn)
        {
            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) { AdvanceBurstIndex(); return false; }
            var cfg = slot.loadedChip.def.GetModExtension<VerbChipConfig>();
            if (cfg == null) { AdvanceBurstIndex(); return false; }

            int volleyCount = cfg.GetPrimaryBurstCount();
            var fm = GetFireMode(slot.loadedChip);
            if (fm != null) volleyCount = fm.GetEffectiveBurst(volleyCount);

            // 获取使用消耗（统一层）- 每次射击动作消耗，无论发射多少发
            float usageCost = ChipUsageCostHelper.GetUsageCost(slot.loadedChip);
            var trion = pawn?.GetComp<CompTrion>();
            if (usageCost > 0f && (trion == null || trion.Available < usageCost))
            {
                AdvanceBurstIndex();
                return false;
            }

            bool anyHit = false;
            ThingDef originalProjectile = verbProps.defaultProjectile;
            ThingDef sideProjectile = side == SlotSide.LeftHand ? leftProjectileDef : rightProjectileDef;
            float spread = cfg.ranged?.volleySpreadRadius ?? 0f;

            bool needThingRestore = gs.ManualAnchorsActive && !gs.CurrentShotHasPath && gs.SavedThingTarget.IsValid;
            if (needThingRestore) currentTarget = gs.SavedThingTarget;

            try
            {
                if (sideProjectile != null) verbProps.defaultProjectile = sideProjectile;

                for (int i = 0; i < volleyCount; i++)
                {
                    if (spread > 0f) shotOriginOffset = new Vector3(
                        Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                    if (TryCastShotCore(slot.loadedChip)) anyHit = true;
                }
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
                shotOriginOffset = Vector3.zero;
            }

            if (needThingRestore) currentTarget = new LocalTargetInfo(gs.ManualTargetCell);

            // 扣除Trion（统一层）- 每次射击动作消耗
            if (anyHit && usageCost > 0f) trion?.Consume(usageCost);

            // 标记齐射侧已发射
            if (side == SlotSide.LeftHand) leftVolleyFired = true;
            else rightVolleyFired = true;

            AdvanceBurstIndex();
            return anyHit;
        }

        private bool FireShootSide(SlotSide side, CompTriggerBody triggerComp, Pawn pawn)
        {
            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) { AdvanceBurstIndex(); return false; }
            var cfg = slot.loadedChip.def.GetModExtension<VerbChipConfig>();
            if (cfg == null) { AdvanceBurstIndex(); return false; }

            // 获取使用消耗（统一层）
            float cost = ChipUsageCostHelper.GetUsageCost(slot.loadedChip);
            if (cost > 0f)
            {
                var trion = pawn?.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost)
                {
                    AdvanceBurstIndex();
                    return false;
                }
                trion.Consume(cost);
            }

            bool needThingRestore = gs.ManualAnchorsActive && !gs.CurrentShotHasPath && gs.SavedThingTarget.IsValid;
            if (needThingRestore) currentTarget = gs.SavedThingTarget;

            ThingDef originalProjectile = verbProps.defaultProjectile;
            ThingDef sideProjectile = side == SlotSide.LeftHand ? leftProjectileDef : rightProjectileDef;
            bool result;
            try
            {
                if (sideProjectile != null) verbProps.defaultProjectile = sideProjectile;
                result = TryCastShotCore(slot.loadedChip);
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
            }

            if (needThingRestore) currentTarget = new LocalTargetInfo(gs.ManualTargetCell);

            if (side == SlotSide.LeftHand) leftRemaining--;
            else rightRemaining--;

            AdvanceBurstIndex();
            return result;
        }

        private void AdvanceBurstIndex()
        {
            dualBurstIndex++;
            if (burstShotsLeft <= 1) dualBurstIndex = 0;
        }

        private void InitDualBurst()
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null)
            {
                leftRemaining = verbProps.burstShotCount;
                rightRemaining = 0;
                leftProjectileDef = null;
                rightProjectileDef = null;
                gs.LeftHasPath = false;
                gs.RightHasPath = false;
                return;
            }

            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
            var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

            // 判断哪一侧是齐射
            leftIsVolley = IsVolleyChip(leftSlot);
            rightIsVolley = IsVolleyChip(rightSlot);

            // 齐射侧算1发，逐发侧算实际发数
            leftRemaining = leftIsVolley ? 1 : (leftCfg?.GetPrimaryBurstCount() ?? 0);
            rightRemaining = rightIsVolley ? 1 : (rightCfg?.GetPrimaryBurstCount() ?? 0);

            // FireMode注入（只对逐发侧有效）
            if (!leftIsVolley)
            {
                var leftFm = GetFireMode(leftSlot?.loadedChip);
                if (leftFm != null) leftRemaining = leftFm.GetEffectiveBurst(leftRemaining);
            }
            if (!rightIsVolley)
            {
                var rightFm = GetFireMode(rightSlot?.loadedChip);
                if (rightFm != null) rightRemaining = rightFm.GetEffectiveBurst(rightRemaining);
            }

            // ── 架构修正：在初始化时检查每一侧的LOS ──
            // 获取最终目标Cell（引导模式下使用SavedThingTarget，否则使用currentTarget）
            IntVec3 finalTargetCell = gs.SavedThingTarget.IsValid
                ? gs.SavedThingTarget.Cell
                : currentTarget.Cell;

            // 左侧LOS检查：如果不支持引导且无直视LOS，排除这一侧
            if (leftCfg != null && leftRemaining > 0 && leftCfg.ranged?.guided == null)
            {
                if (!GenSight.LineOfSight(caster.Position, finalTargetCell, caster.Map))
                {
                    leftRemaining = 0;
                }
            }

            // 右侧LOS检查：如果不支持引导且无直视LOS，排除这一侧
            if (rightCfg != null && rightRemaining > 0 && rightCfg.ranged?.guided == null)
            {
                if (!GenSight.LineOfSight(caster.Position, finalTargetCell, caster.Map))
                {
                    rightRemaining = 0;
                }
            }

            verbProps.burstShotCount = leftRemaining + rightRemaining;
            leftProjectileDef = leftCfg?.GetPrimaryProjectileDef();
            rightProjectileDef = rightCfg?.GetPrimaryProjectileDef();
            gs.LeftHasPath = leftCfg?.ranged?.guided != null;
            gs.RightHasPath = rightCfg?.ranged?.guided != null;

            leftVolleyFired = false;
            rightVolleyFired = false;
        }

        private bool IsVolleyChip(ChipSlot slot)
        {
            if (slot?.loadedChip == null) return false;
            var cfg = slot.loadedChip.def.GetModExtension<VerbChipConfig>();
            // 检查primaryVerbProps的verbClass是否为Verb_BDPVolley
            return cfg?.primaryVerbProps?.verbClass == typeof(Verb_BDPVolley);
        }

        private SlotSide GetCurrentShotSide()
        {
            if (leftRemaining <= 0 && rightRemaining <= 0)
                return SlotSide.LeftHand;

            // 齐射侧只发射一次
            if (leftIsVolley && leftVolleyFired) leftRemaining = 0;
            if (rightIsVolley && rightVolleyFired) rightRemaining = 0;

            if (leftRemaining > 0 && rightRemaining > 0)
            {
                // 交替发射
                if (dualBurstIndex % 2 == 0)
                    return SlotSide.LeftHand;
                else
                    return SlotSide.RightHand;
            }
            else if (leftRemaining > 0)
                return SlotSide.LeftHand;
            else
                return SlotSide.RightHand;
        }
    }
}
