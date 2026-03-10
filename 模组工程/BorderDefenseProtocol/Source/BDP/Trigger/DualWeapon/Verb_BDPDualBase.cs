using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace BDP.Trigger
{
    /// <summary>
    /// 双侧Verb中间抽象层。
    /// 从Verb_BDPDualRanged、Verb_BDPDualVolley、Verb_BDPDualMixed上提30+处重复代码。
    /// </summary>
    public abstract class Verb_BDPDualBase : Verb_BDPRangedBase
    {
        // ═══════════════════════════════════════════
        //  共享字段 (从三个子类上提)
        // ═══════════════════════════════════════════

        protected int dualBurstIndex;
        protected int leftRemaining;
        protected int rightRemaining;
        protected ThingDef leftProjectileDef;
        protected ThingDef rightProjectileDef;

        // ═══════════════════════════════════════════
        //  完全相同的override方法 (3x4=12处重复)
        // ═══════════════════════════════════════════

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

        protected override ThingDef GetAutoRouteProjectileDef()
        {
            var tc = GetTriggerComp();
            var leftCfg = tc?.GetActiveSlot(SlotSide.LeftHand)
                ?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            var rightCfg = tc?.GetActiveSlot(SlotSide.RightHand)
                ?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

            if (leftCfg?.ranged?.guided != null)
                return leftCfg.GetPrimaryProjectileDef() ?? base.GetAutoRouteProjectileDef();
            if (rightCfg?.ranged?.guided != null)
                return rightCfg.GetPrimaryProjectileDef() ?? base.GetAutoRouteProjectileDef();

            return leftCfg?.GetPrimaryProjectileDef()
                ?? rightCfg?.GetPrimaryProjectileDef()
                ?? base.GetAutoRouteProjectileDef();
        }

        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (gs.ManualAnchorsActive && gs.CurrentShotHasPath)
                gs.AttachManualFlight(proj);
            else
                gs.AttachAutoRouteFlight(proj, gs.ResolveAutoRouteFinalTarget(currentTarget),
                    GetGuidedConfig()?.ranged?.guided?.anchorSpread ?? 0.3f);
        }

        protected override LocalTargetInfo GetLosCheckTarget()
        {
            return gs.GetDualLosCheckTarget(currentTarget);
        }

        // ═══════════════════════════════════════════
        //  辅助方法 (从子类提取的共享逻辑)
        // ═══════════════════════════════════════════

        /// <summary>
        /// 判断指定侧是否有引导路径。
        /// </summary>
        protected bool IsSideGuided(SlotSide side)
        {
            return side == SlotSide.LeftHand ? gs.LeftHasPath : gs.RightHasPath;
        }

        /// <summary>
        /// 在目标恢复上下文中执行action。
        /// 用于手动锚点模式下的目标临时切换。
        /// </summary>
        protected void WithTargetRestore(Action action)
        {
            bool needRestore = gs.ManualAnchorsActive && !gs.CurrentShotHasPath && gs.SavedThingTarget.IsValid;
            LocalTargetInfo savedTarget = currentTarget;

            if (needRestore)
                currentTarget = gs.SavedThingTarget;

            try
            {
                action();
            }
            finally
            {
                if (needRestore)
                    currentTarget = savedTarget;
            }
        }

        /// <summary>
        /// 在投射物def临时替换上下文中执行func。
        /// 用于双侧发射时切换不同的投射物类型。
        /// </summary>
        protected T WithProjectileSwap<T>(ThingDef sideProjectile, Func<T> func)
        {
            ThingDef originalProjectile = verbProps.defaultProjectile;
            try
            {
                if (sideProjectile != null)
                    verbProps.defaultProjectile = sideProjectile;
                return func();
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
            }
        }

        /// <summary>
        /// 执行齐射循环发射。
        /// </summary>
        protected bool FireVolleyLoop(int volleyCount, float spread, Thing chipEquipment)
        {
            bool anyHit = false;
            for (int i = 0; i < volleyCount; i++)
            {
                if (spread > 0f)
                    shotOriginOffset = new Vector3(
                        Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));

                if (TryCastShotCore(chipEquipment))
                    anyHit = true;
            }
            shotOriginOffset = Vector3.zero;
            return anyHit;
        }

        // ═══════════════════════════════════════════
        //  Reset (从子类上提)
        // ═══════════════════════════════════════════

        public override void Reset()
        {
            base.Reset();
            dualBurstIndex = 0;
            leftRemaining = 0;
            rightRemaining = 0;
            leftProjectileDef = null;
            rightProjectileDef = null;
        }
    }
}
