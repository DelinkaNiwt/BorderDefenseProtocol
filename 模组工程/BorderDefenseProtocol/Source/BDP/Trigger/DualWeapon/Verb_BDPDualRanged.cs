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
    /// v10.0: 继承Verb_BDPDualBase,消除与其他双侧Verb的重复代码。
    /// </summary>
    public class Verb_BDPDualRanged : Verb_BDPDualBase
    {

        /// <summary>查找两侧中支持引导的芯片配置。</summary>
        protected override VerbChipConfig GetGuidedConfig()
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return null;
            var leftCfg = triggerComp.GetActiveSlot(SlotSide.LeftHand)
                ?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            var rightCfg = triggerComp.GetActiveSlot(SlotSide.RightHand)
                ?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            if (leftCfg?.ranged?.guided != null) return leftCfg;
            if (rightCfg?.ranged?.guided != null) return rightCfg;
            return null;
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

            gs.ResetAutoRouteCastState();  // 清除上次 burst 残留的 autoRouteLosCell，防止误重定向
            LocalTargetInfo actualTarget = gs.InterceptDualCastTarget(
                ref castTarg, caster.Position, caster.Map);

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            if (result)
                gs.PostDualCastOn(ref currentTarget, actualTarget);

            return result;
        }

        protected override ThingDef GetAutoRouteProjectileDef()
        {
            var triggerComp = GetTriggerComp();
            var leftCfg = triggerComp?.GetActiveSlot(SlotSide.LeftHand)?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            var rightCfg = triggerComp?.GetActiveSlot(SlotSide.RightHand)?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

            // 自动绕行只对引导弹有意义：优先返回支持引导的一侧弹药。
            if (leftCfg?.ranged?.guided != null)
                return leftCfg.GetPrimaryProjectileDef() ?? base.GetAutoRouteProjectileDef();
            if (rightCfg?.ranged?.guided != null)
                return rightCfg.GetPrimaryProjectileDef() ?? base.GetAutoRouteProjectileDef();

            return leftCfg?.GetPrimaryProjectileDef()
                ?? rightCfg?.GetPrimaryProjectileDef()
                ?? base.GetAutoRouteProjectileDef();
        }

        /// <summary>双侧LOS检查：感知CurrentShotHasPath。</summary>
        protected override LocalTargetInfo GetLosCheckTarget()
            => gs.GetDualLosCheckTarget(currentTarget);

        /// <summary>
        /// 双侧弹道发射回调：
        /// - 手动引导：仅当前发属于引导侧时附加手动锚点路径；
        /// - 自动绕行：与单侧保持一致，走同一套自动路径挂载流程。
        /// </summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (gs.ManualAnchorsActive && gs.CurrentShotHasPath)
                gs.AttachManualFlight(proj);
            else
                gs.AttachAutoRouteFlight(proj, gs.ResolveAutoRouteFinalTarget(currentTarget),
                    GetGuidedConfig()?.ranged?.guided?.anchorSpread ?? 0.3f);
        }

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
            gs.CurrentShotHasPath = gs.ManualAnchorsActive && IsSideGuided(shotSide);

            float cost = GetSideTrionCost(shotSide);
            if (cost > 0f)
            {
                var trion = CasterPawn?.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost) return false;
                trion.Consume(cost);
            }

            Thing chipEquipment = GetSideChipThing(shotSide);

            bool needThingRestore = gs.ManualAnchorsActive
                && !gs.CurrentShotHasPath && gs.SavedThingTarget.IsValid;
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
                currentTarget = new LocalTargetInfo(gs.ManualTargetCell);

            dualBurstIndex++;
            if (burstShotsLeft <= 1) dualBurstIndex = 0;

            return result;
        }

        private bool IsSideGuided(SlotSide side)
            => side == SlotSide.LeftHand ? gs.LeftHasPath : gs.RightHasPath;

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

            var leftCfg = triggerComp.GetActiveSlot(SlotSide.LeftHand)
                ?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            var rightCfg = triggerComp.GetActiveSlot(SlotSide.RightHand)
                ?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

            leftRemaining = leftCfg?.GetPrimaryBurstCount() ?? 0;
            rightRemaining = rightCfg?.GetPrimaryBurstCount() ?? 0;
            // v9.0 FireMode：连射数注入
            var leftFm  = GetFireMode(triggerComp.GetActiveSlot(SlotSide.LeftHand)?.loadedChip);
            var rightFm = GetFireMode(triggerComp.GetActiveSlot(SlotSide.RightHand)?.loadedChip);
            if (leftFm  != null) leftRemaining  = leftFm.GetEffectiveBurst(leftRemaining);
            if (rightFm != null) rightRemaining = rightFm.GetEffectiveBurst(rightRemaining);

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

            verbProps.burstShotCount = leftRemaining + rightRemaining; // 同步引擎总发数
            leftProjectileDef = leftCfg?.GetPrimaryProjectileDef();
            rightProjectileDef = rightCfg?.GetPrimaryProjectileDef();
            gs.LeftHasPath = leftCfg?.ranged?.guided != null;
            gs.RightHasPath = rightCfg?.ranged?.guided != null;
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

            // 使用统一工具类获取使用消耗
            return ChipUsageCostHelper.GetUsageCost(slot.loadedChip);
        }

        private Thing GetSideChipThing(SlotSide side)
        {
            var triggerComp = GetTriggerComp();
            return triggerComp?.GetActiveSlot(side)?.loadedChip;
        }
    }
}
