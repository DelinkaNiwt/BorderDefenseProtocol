using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 远程交替连射Verb（v2.0 §8.6，v4.0 B3远程修复）——交替子弹类型，叠加连射数。
    /// 继承Verb_BDPRangedBase，调用TryCastShotCore(chipThing)使战斗日志显示芯片名。
    ///
    /// 连射数合成：总burstShotCount = 左连射数 + 右连射数
    /// 子弹交替规则：L, R, L, R... 直到一方用完，剩余补齐
    ///
    /// v7.0变化弹适配。Fix-2：引导状态委托给GuidedVerbState组合类。
    /// </summary>
    public class Verb_BDPDualRanged : Verb_BDPRangedBase
    {
        private int dualBurstIndex = 0;
        private int leftRemaining = 0;
        private int rightRemaining = 0;
        private ThingDef leftProjectileDef;
        private ThingDef rightProjectileDef;

        /// <summary>引导弹共享状态（Fix-2）。</summary>
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

        protected override bool TryCastShot()
        {
            if (dualBurstIndex == 0)
                InitDualBurst();

            SlotSide shotSide = GetCurrentShotSide();
            gs.CurrentShotIsGuided = gs.GuidedActive && IsSideGuided(shotSide);

            // 引导模式下，非变化弹侧无直视LOS → 跳过这发
            if (gs.GuidedActive && !gs.CurrentShotIsGuided)
            {
                IntVec3 losCell = gs.SavedThingTarget.IsValid ? gs.SavedThingTarget.Cell : currentTarget.Cell;
                if (!GenSight.LineOfSight(caster.Position, losCell, caster.Map))
                {
                    dualBurstIndex++;
                    if (burstShotsLeft <= 1) dualBurstIndex = 0;
                    return true;
                }
            }

            float cost = GetSideTrionCost(shotSide);
            if (cost > 0f)
            {
                var trion = CasterPawn?.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost) return false;
                trion.Consume(cost);
            }

            Thing chipEquipment = GetSideChipThing(shotSide);

            bool needThingRestore = gs.GuidedActive && !gs.CurrentShotIsGuided && gs.SavedThingTarget.IsValid;
            if (needThingRestore) currentTarget = gs.SavedThingTarget;

            ThingDef originalProjectile = verbProps.defaultProjectile;
            ThingDef sideProjectile = shotSide == SlotSide.LeftHand ? leftProjectileDef : rightProjectileDef;
            bool result;
            try
            {
                if (sideProjectile != null) verbProps.defaultProjectile = sideProjectile;
                result = TryCastShotCore(chipEquipment);
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
            }

            if (needThingRestore) currentTarget = new LocalTargetInfo(gs.GuidedTargetCell);

            dualBurstIndex++;
            if (burstShotsLeft <= 1) dualBurstIndex = 0;

            return result;
        }

        private bool IsSideGuided(SlotSide side)
            => side == SlotSide.LeftHand ? gs.LeftIsGuided : gs.RightIsGuided;

        private void InitDualBurst()
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null)
            {
                leftRemaining = verbProps.burstShotCount;
                rightRemaining = 0;
                leftProjectileDef = null;
                rightProjectileDef = null;
                gs.LeftIsGuided = false;
                gs.RightIsGuided = false;
                return;
            }

            var leftCfg = triggerComp.GetActiveSlot(SlotSide.LeftHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = triggerComp.GetActiveSlot(SlotSide.RightHand)?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();

            leftRemaining = leftCfg?.GetFirstBurstCount() ?? 0;
            rightRemaining = rightCfg?.GetFirstBurstCount() ?? 0;
            leftProjectileDef = leftCfg?.GetFirstProjectileDef();
            rightProjectileDef = rightCfg?.GetFirstProjectileDef();
            gs.LeftIsGuided = leftCfg?.supportsGuided == true;
            gs.RightIsGuided = rightCfg?.supportsGuided == true;
        }

        /// <summary>确定当前发应使用哪一侧（Fix-12：双零防护）。</summary>
        private SlotSide GetCurrentShotSide()
        {
            if (leftRemaining <= 0 && rightRemaining <= 0)
                return SlotSide.LeftHand;
            if (leftRemaining > 0 && rightRemaining > 0)
            {
                if (dualBurstIndex % 2 == 0) { leftRemaining--; return SlotSide.LeftHand; }
                else { rightRemaining--; return SlotSide.RightHand; }
            }
            else if (leftRemaining > 0) { leftRemaining--; return SlotSide.LeftHand; }
            else { rightRemaining--; return SlotSide.RightHand; }
        }

        private float GetSideTrionCost(SlotSide side)
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return 0f;
            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) return 0f;
            return slot.loadedChip.def.GetModExtension<WeaponChipConfig>()?.trionCostPerShot ?? 0f;
        }

        private Thing GetSideChipThing(SlotSide side)
        {
            var triggerComp = GetTriggerComp();
            return triggerComp?.GetActiveSlot(side)?.loadedChip;
        }
    }
}
