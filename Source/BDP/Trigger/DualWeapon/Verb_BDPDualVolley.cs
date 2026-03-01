using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 双侧齐射Verb（v6.1新增，v8.0 PMS重构）。
    /// 在单次TryCastShot()中发射两侧所有子弹。
    /// </summary>
    public class Verb_BDPDualVolley : Verb_BDPRangedBase
    {
        /// <summary>任一侧是否支持变化弹。</summary>
        public bool HasGuidedSide
        {
            get
            {
                var tc = GetTriggerComp();
                if (tc == null) return false;
                var lc = tc.GetActiveSlot(SlotSide.LeftHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                var rc = tc.GetActiveSlot(SlotSide.RightHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                return (lc?.supportsGuided == true) || (rc?.supportsGuided == true);
            }
        }

        /// <summary>双侧引导瞄准。</summary>
        public override void StartAnchorTargeting()
        {
            var tc = GetTriggerComp();
            if (tc == null) { Find.Targeter.BeginTargeting(this); return; }
            var lc = tc.GetActiveSlot(SlotSide.LeftHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rc = tc.GetActiveSlot(SlotSide.RightHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            WeaponChipConfig gc = (lc?.supportsGuided == true) ? lc : (rc?.supportsGuided == true) ? rc : null;
            if (gc == null) { Find.Targeter.BeginTargeting(this); return; }
            AnchorTargetingHelper.BeginAnchorTargeting(this, CasterPawn, gc.maxAnchors, verbProps.range,
                (anchors, finalTarget) =>
                {
                    gs.StoreTargetingResult(anchors, finalTarget, gc.anchorSpread);
                    gs.LeftHasPath = lc?.supportsGuided == true;
                    gs.RightHasPath = rc?.supportsGuided == true;
                    OrderForceTargetCore(finalTarget);
                });
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (HasGuidedSide) { StartAnchorTargeting(); return; }
            gs.ManualAnchorsActive = false;
            OrderForceTargetCore(target);
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            LocalTargetInfo actualTarget = gs.InterceptDualCastTarget(ref castTarg, caster.Position, caster.Map);
            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
            if (result) gs.PostDualCastOn(ref currentTarget, actualTarget);
            return result;
        }

        protected override ThingDef GetAutoRouteProjectileDef()
        {
            var tc = GetTriggerComp();
            var leftCfg = tc?.GetActiveSlot(SlotSide.LeftHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = tc?.GetActiveSlot(SlotSide.RightHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();

            // 自动绕行只对引导弹有意义：优先返回支持引导的一侧弹药。
            if (leftCfg?.supportsGuided == true)
                return leftCfg.GetFirstProjectileDef() ?? base.GetAutoRouteProjectileDef();
            if (rightCfg?.supportsGuided == true)
                return rightCfg.GetFirstProjectileDef() ?? base.GetAutoRouteProjectileDef();

            return leftCfg?.GetFirstProjectileDef()
                ?? rightCfg?.GetFirstProjectileDef()
                ?? base.GetAutoRouteProjectileDef();
        }

        protected override LocalTargetInfo GetLosCheckTarget() => gs.GetDualLosCheckTarget(currentTarget);

        /// <summary>
        /// 弹道发射后回调：手动引导时走引导路径，否则尝试自动绕行。
        /// </summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (gs.ManualAnchorsActive && gs.CurrentShotHasPath)
                gs.AttachManualFlight(proj);
            else
                gs.AttachAutoRouteFlight(proj, gs.ResolveAutoRouteFinalTarget(currentTarget),
                    GetChipConfig()?.anchorSpread ?? 0.3f);
        }

        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn == null) return false;
            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return false;
            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
            if (leftSlot?.loadedChip == null || rightSlot?.loadedChip == null) return false;
            var leftCfg = leftSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            if (leftCfg == null || rightCfg == null) return false;

            int leftCount = leftCfg.GetFirstBurstCount();
            int rightCount = rightCfg.GetFirstBurstCount();
            // v9.0 FireMode：连射数注入
            var leftFm  = GetFireMode(leftSlot.loadedChip);
            var rightFm = GetFireMode(rightSlot.loadedChip);
            if (leftFm  != null) leftCount  = leftFm.GetEffectiveBurst(leftCount);
            if (rightFm != null) rightCount = rightFm.GetEffectiveBurst(rightCount);
            ThingDef leftProj = leftCfg.GetFirstProjectileDef();
            ThingDef rightProj = rightCfg.GetFirstProjectileDef();

            if (gs.ManualAnchorsActive) { gs.LeftHasPath = leftCfg.supportsGuided; gs.RightHasPath = rightCfg.supportsGuided; }

            bool hasDirectLos = !gs.ManualAnchorsActive || GenSight.LineOfSight(caster.Position,
                gs.SavedThingTarget.IsValid ? gs.SavedThingTarget.Cell : currentTarget.Cell, caster.Map);
            bool leftWillFire = !gs.ManualAnchorsActive || gs.LeftHasPath || hasDirectLos;
            bool rightWillFire = !gs.ManualAnchorsActive || gs.RightHasPath || hasDirectLos;

            float totalCost = (leftWillFire ? leftCount * leftCfg.trionCostPerShot : 0f)
                            + (rightWillFire ? rightCount * rightCfg.trionCostPerShot : 0f);
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost)) return false;

            bool anyHit = false;
            ThingDef originalProjectile = verbProps.defaultProjectile;
            float spread = Mathf.Max(leftCfg.volleySpreadRadius, rightCfg.volleySpreadRadius);

            // ★ 自动绕行：齐射前计算路由（用左侧弹药检查GuidedModule）
            gs.PrepareAutoRoute(caster.Position, currentTarget.Cell,
                caster.Map, leftProj);

            try
            {
                if (leftWillFire)
                {
                    gs.CurrentShotHasPath = gs.ManualAnchorsActive && gs.LeftHasPath;
                    if (leftProj != null) verbProps.defaultProjectile = leftProj;
                    bool needRestoreL = gs.ManualAnchorsActive && !gs.CurrentShotHasPath && gs.SavedThingTarget.IsValid;
                    if (needRestoreL) currentTarget = gs.SavedThingTarget;
                    for (int i = 0; i < leftCount; i++)
                    {
                        if (spread > 0f) shotOriginOffset = new Vector3(Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                        if (TryCastShotCore(leftSlot.loadedChip)) anyHit = true;
                    }
                    if (needRestoreL) currentTarget = new LocalTargetInfo(gs.ManualTargetCell);
                }
                if (rightWillFire)
                {
                    gs.CurrentShotHasPath = gs.ManualAnchorsActive && gs.RightHasPath;
                    if (rightProj != null) verbProps.defaultProjectile = rightProj;
                    bool needRestoreR = gs.ManualAnchorsActive && !gs.CurrentShotHasPath && gs.SavedThingTarget.IsValid;
                    if (needRestoreR) currentTarget = gs.SavedThingTarget;
                    for (int i = 0; i < rightCount; i++)
                    {
                        if (spread > 0f) shotOriginOffset = new Vector3(Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                        if (TryCastShotCore(rightSlot.loadedChip)) anyHit = true;
                    }
                    if (needRestoreR) currentTarget = new LocalTargetInfo(gs.ManualTargetCell);
                }
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
                shotOriginOffset = Vector3.zero;
                gs.CurrentShotHasPath = false;
            }
            if (anyHit && totalCost > 0f) trion?.Consume(totalCost);
            return anyHit;
        }
    }
}
