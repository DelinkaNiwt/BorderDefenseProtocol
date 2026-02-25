using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 远程交替连射Verb（v2.0 §8.6，v4.0 B3远程修复，v8.0 PMS重构）。
    /// 交替子弹类型，叠加连射数。
    ///
    /// 连射数合成：总burstShotCount = 左连射数 + 右连射数
    /// 子弹交替规则：L, R, L, R... 直到一方用完，剩余补齐
    ///
    /// PMS重构：引导弹逻辑由基类Verb_BDPRangedBase统一管理，
    /// 本类仅保留双侧特有的TryStartCastOn/GetLosCheckTarget重写。
    /// </summary>
    public class Verb_BDPDualRanged : Verb_BDPRangedBase
    {
        private int dualBurstIndex = 0;
        private int leftRemaining = 0;
        private int rightRemaining = 0;
        private ThingDef leftProjectileDef;
        private ThingDef rightProjectileDef;

        /// <summary>任一侧是否支持变化弹。</summary>
        public bool HasGuidedSide
        {
            get
            {
                var triggerComp = GetTriggerComp();
                if (triggerComp == null) return false;
                var leftCfg = triggerComp.GetActiveSlot(SlotSide.LeftHand)
                    ?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                var rightCfg = triggerComp.GetActiveSlot(SlotSide.RightHand)
                    ?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                return (leftCfg?.supportsGuided == true)
                    || (rightCfg?.supportsGuided == true);
            }
        }

        /// <summary>双侧引导瞄准——查找任一侧的引导配置。</summary>
        public override void StartGuidedTargeting()
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null) { Find.Targeter.BeginTargeting(this); return; }

            var leftCfg = triggerComp.GetActiveSlot(SlotSide.LeftHand)
                ?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = triggerComp.GetActiveSlot(SlotSide.RightHand)
                ?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            WeaponChipConfig guidedCfg = (leftCfg?.supportsGuided == true) ? leftCfg
                                       : (rightCfg?.supportsGuided == true) ? rightCfg : null;
            if (guidedCfg == null) { Find.Targeter.BeginTargeting(this); return; }

            GuidedTargetingHelper.BeginGuidedTargeting(
                this, CasterPawn, guidedCfg.maxAnchors, verbProps.range,
                (anchors, finalTarget) =>
                {
                    gs.StoreTargetingResult(anchors, finalTarget, guidedCfg.anchorSpread);
                    OrderForceTargetCore(finalTarget);
                });
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (HasGuidedSide) { StartGuidedTargeting(); return; }
            gs.GuidedActive = false;
            OrderForceTargetCore(target);
        }

        /// <summary>
        /// 双侧TryStartCastOn：每次新burst开始时重置双武器状态，
        /// 然后使用双侧专用的InterceptDualCastTarget。
        ///
        /// 重置原因：burst被外部中断（目标摧毁、LOS丢失）时，
        /// TryCastNextBurstShot直接设burstShotsLeft=0结束burst，
        /// 不调用Reset()，导致dualBurstIndex/remaining/projectileDef
        /// 残留到下次攻击，InitDualBurst()不被调用，弹药类型错乱。
        /// </summary>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            // 每次新burst开始时清理残留状态
            dualBurstIndex = 0;
            leftRemaining = 0;
            rightRemaining = 0;
            leftProjectileDef = null;
            rightProjectileDef = null;

            LocalTargetInfo actualTarget = gs.InterceptDualCastTarget(
                ref castTarg, caster.Position, caster.Map);

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            if (result)
                gs.PostDualCastOn(ref currentTarget, actualTarget);

            return result;
        }

        /// <summary>双侧LOS检查：感知CurrentShotIsGuided。</summary>
        protected override LocalTargetInfo GetLosCheckTarget()
            => gs.GetDualLosCheckTarget(currentTarget);

        /// <summary>双侧弹道发射回调：仅当前发属于变化弹侧时附加引导。</summary>
        protected override void OnProjectileLaunched(Projectile proj)
            => gs.AttachGuidedFlightIfActive(proj);

        /// <summary>
        /// 重写Reset：清理双武器burst残留状态。
        /// 原因：原版Verb.Reset()不知道dualBurstIndex等字段，
        /// 当burst被外部中断（目标摧毁、LOS丢失等）时，
        /// TryCastNextBurstShot直接设burstShotsLeft=0结束burst，
        /// 但dualBurstIndex/remaining/projectileDef未被清理，
        /// 导致下次攻击InitDualBurst()不被调用，使用残留状态。
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            dualBurstIndex = 0;
            leftRemaining = 0;
            rightRemaining = 0;
            leftProjectileDef = null;
            rightProjectileDef = null;
        }

        protected override bool TryCastShot()
        {
            if (dualBurstIndex == 0)
                InitDualBurst();

            SlotSide shotSide = GetCurrentShotSide();
            gs.CurrentShotIsGuided = gs.GuidedActive && IsSideGuided(shotSide);

            // 引导模式下，非变化弹侧无直视LOS → 跳过这发
            if (gs.GuidedActive && !gs.CurrentShotIsGuided)
            {
                IntVec3 losCell = gs.SavedThingTarget.IsValid
                    ? gs.SavedThingTarget.Cell : currentTarget.Cell;
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

            bool needThingRestore = gs.GuidedActive
                && !gs.CurrentShotIsGuided && gs.SavedThingTarget.IsValid;
            if (needThingRestore) currentTarget = gs.SavedThingTarget;

            ThingDef originalProjectile = verbProps.defaultProjectile;
            ThingDef sideProjectile = shotSide == SlotSide.LeftHand
                ? leftProjectileDef : rightProjectileDef;
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

            if (needThingRestore)
                currentTarget = new LocalTargetInfo(gs.GuidedTargetCell);

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

            var leftCfg = triggerComp.GetActiveSlot(SlotSide.LeftHand)
                ?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = triggerComp.GetActiveSlot(SlotSide.RightHand)
                ?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();

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
                if (dualBurstIndex % 2 == 0)
                    { leftRemaining--; return SlotSide.LeftHand; }
                else
                    { rightRemaining--; return SlotSide.RightHand; }
            }
            else if (leftRemaining > 0)
                { leftRemaining--; return SlotSide.LeftHand; }
            else
                { rightRemaining--; return SlotSide.RightHand; }
        }

        private float GetSideTrionCost(SlotSide side)
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return 0f;
            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) return 0f;
            return slot.loadedChip.def.GetModExtension<WeaponChipConfig>()
                ?.trionCostPerShot ?? 0f;
        }

        private Thing GetSideChipThing(SlotSide side)
        {
            var triggerComp = GetTriggerComp();
            return triggerComp?.GetActiveSlot(side)?.loadedChip;
        }
    }
}
