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
    public class Verb_BDPDualVolley : Verb_BDPDualBase
    {
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

            // 获取使用消耗（统一层）- 每次射击动作消耗，无论发射多少发
            float leftUsageCost = ChipUsageCostHelper.GetUsageCost(leftSlot.loadedChip);
            float rightUsageCost = ChipUsageCostHelper.GetUsageCost(rightSlot.loadedChip);
            float totalCost = (leftWillFire ? leftUsageCost : 0f) + (rightWillFire ? rightUsageCost : 0f);
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
