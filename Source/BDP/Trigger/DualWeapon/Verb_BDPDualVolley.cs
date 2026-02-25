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
        public override void StartGuidedTargeting()
        {
            var tc = GetTriggerComp();
            if (tc == null) { Find.Targeter.BeginTargeting(this); return; }
            var lc = tc.GetActiveSlot(SlotSide.LeftHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rc = tc.GetActiveSlot(SlotSide.RightHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            WeaponChipConfig gc = (lc?.supportsGuided == true) ? lc : (rc?.supportsGuided == true) ? rc : null;
            if (gc == null) { Find.Targeter.BeginTargeting(this); return; }
            GuidedTargetingHelper.BeginGuidedTargeting(this, CasterPawn, gc.maxAnchors, verbProps.range,
                (anchors, finalTarget) =>
                {
                    gs.StoreTargetingResult(anchors, finalTarget, gc.anchorSpread);
                    gs.LeftIsGuided = lc?.supportsGuided == true;
                    gs.RightIsGuided = rc?.supportsGuided == true;
                    OrderForceTargetCore(finalTarget);
                });
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (HasGuidedSide) { StartGuidedTargeting(); return; }
            gs.GuidedActive = false;
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

        protected override LocalTargetInfo GetLosCheckTarget() => gs.GetDualLosCheckTarget(currentTarget);
        protected override void OnProjectileLaunched(Projectile proj) => gs.AttachGuidedFlightIfActive(proj);

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
            ThingDef leftProj = leftCfg.GetFirstProjectileDef();
            ThingDef rightProj = rightCfg.GetFirstProjectileDef();

            if (gs.GuidedActive) { gs.LeftIsGuided = leftCfg.supportsGuided; gs.RightIsGuided = rightCfg.supportsGuided; }

            bool hasDirectLos = !gs.GuidedActive || GenSight.LineOfSight(caster.Position,
                gs.SavedThingTarget.IsValid ? gs.SavedThingTarget.Cell : currentTarget.Cell, caster.Map);
            bool leftWillFire = !gs.GuidedActive || gs.LeftIsGuided || hasDirectLos;
            bool rightWillFire = !gs.GuidedActive || gs.RightIsGuided || hasDirectLos;

            float totalCost = (leftWillFire ? leftCount * leftCfg.trionCostPerShot : 0f)
                            + (rightWillFire ? rightCount * rightCfg.trionCostPerShot : 0f);
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost)) return false;

            bool anyHit = false;
            ThingDef originalProjectile = verbProps.defaultProjectile;
            float spread = Mathf.Max(leftCfg.volleySpreadRadius, rightCfg.volleySpreadRadius);
            try
            {
                if (leftWillFire)
                {
                    gs.CurrentShotIsGuided = gs.GuidedActive && gs.LeftIsGuided;
                    if (leftProj != null) verbProps.defaultProjectile = leftProj;
                    bool needRestoreL = gs.GuidedActive && !gs.CurrentShotIsGuided && gs.SavedThingTarget.IsValid;
                    if (needRestoreL) currentTarget = gs.SavedThingTarget;
                    for (int i = 0; i < leftCount; i++)
                    {
                        if (spread > 0f) shotOriginOffset = new Vector3(Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                        if (TryCastShotCore(leftSlot.loadedChip)) anyHit = true;
                    }
                    if (needRestoreL) currentTarget = new LocalTargetInfo(gs.GuidedTargetCell);
                }
                if (rightWillFire)
                {
                    gs.CurrentShotIsGuided = gs.GuidedActive && gs.RightIsGuided;
                    if (rightProj != null) verbProps.defaultProjectile = rightProj;
                    bool needRestoreR = gs.GuidedActive && !gs.CurrentShotIsGuided && gs.SavedThingTarget.IsValid;
                    if (needRestoreR) currentTarget = gs.SavedThingTarget;
                    for (int i = 0; i < rightCount; i++)
                    {
                        if (spread > 0f) shotOriginOffset = new Vector3(Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                        if (TryCastShotCore(rightSlot.loadedChip)) anyHit = true;
                    }
                    if (needRestoreR) currentTarget = new LocalTargetInfo(gs.GuidedTargetCell);
                }
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
                shotOriginOffset = Vector3.zero;
                gs.CurrentShotIsGuided = false;
            }
            if (anyHit && totalCost > 0f) trion?.Consume(totalCost);
            return anyHit;
        }
    }
}
