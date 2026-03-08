using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 双侧齐射Verb（v6.1新增，v8.0 PMS重构，v11.0更新为VerbChipConfig）。
    /// 在单次TryCastShot()中发射两侧所有子弹。
    /// 支持引导模块：只有带引导模块的一侧才能使用手动引导路径。
    /// </summary>
    public class Verb_BDPDualVolley : Verb_BDPRangedBase
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
            // 优先返回左侧，如果左侧不支持则返回右侧
            if (leftCfg?.ranged?.guided != null) return leftCfg;
            if (rightCfg?.ranged?.guided != null) return rightCfg;
            return null;
        }

        protected override ThingDef GetAutoRouteProjectileDef()
        {
            var tc = GetTriggerComp();
            var leftCfg = tc?.GetActiveSlot(SlotSide.LeftHand)?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            var rightCfg = tc?.GetActiveSlot(SlotSide.RightHand)?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

            // 自动绕行只对引导弹有意义：优先返回支持引导的一侧弹药
            if (leftCfg?.ranged?.guided != null)
                return leftCfg.GetPrimaryProjectileDef() ?? base.GetAutoRouteProjectileDef();
            if (rightCfg?.ranged?.guided != null)
                return rightCfg.GetPrimaryProjectileDef() ?? base.GetAutoRouteProjectileDef();

            return leftCfg?.GetPrimaryProjectileDef()
                ?? rightCfg?.GetPrimaryProjectileDef()
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
                    GetGuidedConfig()?.ranged?.guided?.anchorSpread ?? 0.3f);
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            gs.ResetAutoRouteCastState();
            LocalTargetInfo actualTarget = gs.InterceptDualCastTarget(ref castTarg, caster.Position, caster.Map);
            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
            if (result) gs.PostDualCastOn(ref currentTarget, actualTarget);
            return result;
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
            var leftCfg = leftSlot.loadedChip.def.GetModExtension<VerbChipConfig>();
            var rightCfg = rightSlot.loadedChip.def.GetModExtension<VerbChipConfig>();
            if (leftCfg == null || rightCfg == null) return false;

            int leftCount = leftCfg.GetPrimaryBurstCount();
            int rightCount = rightCfg.GetPrimaryBurstCount();
            var leftFm  = GetFireMode(leftSlot.loadedChip);
            var rightFm = GetFireMode(rightSlot.loadedChip);
            if (leftFm  != null) leftCount  = leftFm.GetEffectiveBurst(leftCount);
            if (rightFm != null) rightCount = rightFm.GetEffectiveBurst(rightCount);
            ThingDef leftProj = leftCfg.GetPrimaryProjectileDef();
            ThingDef rightProj = rightCfg.GetPrimaryProjectileDef();

            // 引导模块支持：标记哪一侧有引导路径
            if (gs.ManualAnchorsActive)
            {
                gs.LeftHasPath = leftCfg.ranged?.guided != null;
                gs.RightHasPath = rightCfg.ranged?.guided != null;
            }

            // 检查LOS：引导模式下，非引导侧需要直视LOS才能发射
            bool hasDirectLos = !gs.ManualAnchorsActive || GenSight.LineOfSight(caster.Position,
                gs.SavedThingTarget.IsValid ? gs.SavedThingTarget.Cell : currentTarget.Cell, caster.Map);
            bool leftWillFire = !gs.ManualAnchorsActive || gs.LeftHasPath || hasDirectLos;
            bool rightWillFire = !gs.ManualAnchorsActive || gs.RightHasPath || hasDirectLos;

            float totalCost = (leftWillFire ? leftCount * (leftCfg.cost?.trionPerShot ?? 0f) : 0f)
                            + (rightWillFire ? rightCount * (rightCfg.cost?.trionPerShot ?? 0f) : 0f);
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost)) return false;

            bool anyHit = false;
            ThingDef originalProjectile = verbProps.defaultProjectile;
            float spread = Mathf.Max(leftCfg.ranged?.volleySpreadRadius ?? 0f, rightCfg.ranged?.volleySpreadRadius ?? 0f);

            gs.PrepareAutoRoute(caster.Position, currentTarget.Cell, caster.Map, leftProj);

            try
            {
                // 左侧齐射
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

                // 右侧齐射
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
