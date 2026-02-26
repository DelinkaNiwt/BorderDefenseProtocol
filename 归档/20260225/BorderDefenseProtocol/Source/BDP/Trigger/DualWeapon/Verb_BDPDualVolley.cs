using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 双侧齐射Verb——在单次TryCastShot()中发射两侧所有子弹（v6.1新增）。
    /// 继承Verb_BDPRangedBase，复用OrderForceTarget（BDP_ChipRangedAttack job）。
    ///
    /// v7.0变化弹适配。Fix-2：引导状态委托给GuidedVerbState组合类。
    /// </summary>
    public class Verb_BDPDualVolley : Verb_BDPRangedBase
    {
        /// <summary>引导弹共享状态（Fix-2：组合模式替代重复字段）。</summary>
        private readonly GuidedVerbState gs = new GuidedVerbState();

        /// <summary>任一侧是否支持变化弹。</summary>
        public bool HasGuidedSide
        {
            get
            {
                var triggerComp = GetTriggerComp();
                if (triggerComp == null) return false;
                var leftCfg = triggerComp.GetActiveSlot(SlotSide.LeftHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                var rightCfg = triggerComp.GetActiveSlot(SlotSide.RightHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                return (leftCfg?.supportsGuided == true) || (rightCfg?.supportsGuided == true);
            }
        }

        /// <summary>启动多步锚点瞄准。</summary>
        public void StartGuidedTargeting()
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null) { Find.Targeter.BeginTargeting(this); return; }

            var leftCfg = triggerComp.GetActiveSlot(SlotSide.LeftHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = triggerComp.GetActiveSlot(SlotSide.RightHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            WeaponChipConfig guidedCfg = (leftCfg?.supportsGuided == true) ? leftCfg
                                       : (rightCfg?.supportsGuided == true) ? rightCfg : null;
            if (guidedCfg == null) { Find.Targeter.BeginTargeting(this); return; }

            GuidedTargetingHelper.BeginGuidedTargeting(
                this, CasterPawn, guidedCfg.maxAnchors, verbProps.range,
                (anchors, finalTarget) =>
                {
                    gs.StoreTargetingResult(anchors, finalTarget, guidedCfg.anchorSpread);
                    gs.LeftIsGuided = leftCfg?.supportsGuided == true;
                    gs.RightIsGuided = rightCfg?.supportsGuided == true;
                    base.OrderForceTarget(finalTarget);
                });
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (HasGuidedSide) { StartGuidedTargeting(); return; }
            gs.GuidedActive = false;
            base.OrderForceTarget(target);
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            LocalTargetInfo actualTarget = gs.InterceptDualCastTarget(
                ref castTarg, caster.Position, caster.Map);

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            if (result)
                gs.PostDualCastOn(ref currentTarget, actualTarget);

            return result;
        }

        protected override LocalTargetInfo GetLosCheckTarget()
            => gs.GetDualLosCheckTarget(base.GetLosCheckTarget());

        protected override void OnProjectileLaunched(Projectile proj)
            => gs.AttachGuidedFlightIfActive(proj);

        /// <summary>齐射TryCastShot：在单次调用中发射两侧所有子弹。</summary>
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

            if (gs.GuidedActive)
            {
                gs.LeftIsGuided = leftCfg.supportsGuided;
                gs.RightIsGuided = rightCfg.supportsGuided;
            }

            bool hasDirectLos = !gs.GuidedActive || GenSight.LineOfSight(
                caster.Position,
                gs.SavedThingTarget.IsValid ? gs.SavedThingTarget.Cell : currentTarget.Cell,
                caster.Map);
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
                    bool needThingRestoreL = gs.GuidedActive && !gs.CurrentShotIsGuided && gs.SavedThingTarget.IsValid;
                    if (needThingRestoreL) currentTarget = gs.SavedThingTarget;
                    for (int i = 0; i < leftCount; i++)
                    {
                        if (spread > 0f)
                            shotOriginOffset = new Vector3(Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                        if (TryCastShotCore(leftSlot.loadedChip)) anyHit = true;
                    }
                    if (needThingRestoreL) currentTarget = new LocalTargetInfo(gs.GuidedTargetCell);
                }

                if (rightWillFire)
                {
                    gs.CurrentShotIsGuided = gs.GuidedActive && gs.RightIsGuided;
                    if (rightProj != null) verbProps.defaultProjectile = rightProj;
                    bool needThingRestoreR = gs.GuidedActive && !gs.CurrentShotIsGuided && gs.SavedThingTarget.IsValid;
                    if (needThingRestoreR) currentTarget = gs.SavedThingTarget;
                    for (int i = 0; i < rightCount; i++)
                    {
                        if (spread > 0f)
                            shotOriginOffset = new Vector3(Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                        if (TryCastShotCore(rightSlot.loadedChip)) anyHit = true;
                    }
                    if (needThingRestoreR) currentTarget = new LocalTargetInfo(gs.GuidedTargetCell);
                }
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
                shotOriginOffset = Vector3.zero;
                gs.CurrentShotIsGuided = false;
            }

            if (anyHit && totalCost > 0f)
                trion?.Consume(totalCost);

            return anyHit;
        }
    }
}
