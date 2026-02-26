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
    /// 与Verb_BDPDualRanged的区别：
    ///   · Verb_BDPDualRanged：引擎burst机制交替逐发射击
    ///   · Verb_BDPDualVolley：burstShotCount=1，TryCastShot内循环发射两侧所有子弹
    ///
    /// v7.0变化弹适配：任一侧supportsGuided=true时，齐射走锚点瞄准逻辑。
    ///   确认目标后：变化弹侧走锚点折线，非变化弹侧直射（无LOS则跳过整个循环）。
    /// </summary>
    public class Verb_BDPDualVolley : Verb_BDPRangedBase
    {
        // ── v7.0变化弹引导支持 ──
        /// <summary>锚点原始坐标（未散布）。</summary>
        private List<IntVec3> rawAnchors;
        /// <summary>最终目标。</summary>
        private LocalTargetInfo rawFinalTarget;
        /// <summary>芯片散布半径缓存。</summary>
        private float cachedAnchorSpread;
        /// <summary>是否处于引导模式。</summary>
        private bool guidedActive;
        /// <summary>瞄准确认时快照的目标地格（不随Thing移动）。</summary>
        private IntVec3 guidedTargetCell;
        /// <summary>原始Thing目标引用（供非变化弹侧跟踪）。</summary>
        private LocalTargetInfo guidedSavedThingTarget;
        /// <summary>左侧是否为变化弹。</summary>
        private bool leftIsGuided;
        /// <summary>右侧是否为变化弹。</summary>
        private bool rightIsGuided;
        /// <summary>当前发射的子弹是否属于变化弹侧（per-shot标记）。</summary>
        private bool currentShotIsGuided;

        /// <summary>任一侧是否支持变化弹（供Gizmo判断是否启动锚点瞄准）。</summary>
        public bool HasGuidedSide
        {
            get
            {
                var pawn = CasterPawn;
                var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
                if (triggerComp == null) return false;
                var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
                var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
                var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                return (leftCfg?.supportsGuided == true) || (rightCfg?.supportsGuided == true);
            }
        }

        /// <summary>
        /// 启动多步锚点瞄准（由Command_BDPChipAttack右键调用）。
        /// 取第一个supportsGuided=true侧的maxAnchors/anchorSpread配置。
        /// </summary>
        public void StartGuidedTargeting()
        {
            var pawn = CasterPawn;
            var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) { Find.Targeter.BeginTargeting(this); return; }

            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
            var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            WeaponChipConfig guidedCfg = (leftCfg?.supportsGuided == true) ? leftCfg
                                       : (rightCfg?.supportsGuided == true) ? rightCfg : null;
            if (guidedCfg == null) { Find.Targeter.BeginTargeting(this); return; }

            GuidedTargetingHelper.BeginGuidedTargeting(
                this, pawn, guidedCfg.maxAnchors, verbProps.range,
                (anchors, finalTarget) =>
                {
                    rawAnchors = new List<IntVec3>(anchors);
                    rawFinalTarget = new LocalTargetInfo(finalTarget.Cell); // Cell-only，断开Thing引用
                    cachedAnchorSpread = guidedCfg.anchorSpread;
                    guidedTargetCell = finalTarget.Cell;           // 快照地格
                    guidedActive = anchors.Count > 0;              // 无锚点=直射模式
                    // 读取两侧引导标记
                    leftIsGuided = leftCfg?.supportsGuided == true;
                    rightIsGuided = rightCfg?.supportsGuided == true;
                    base.OrderForceTarget(finalTarget);
                });
        }

        /// <summary>引导模式下拦截，启动锚点瞄准。</summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (HasGuidedSide)
            {
                StartGuidedTargeting();
                return;
            }
            guidedActive = false;
            base.OrderForceTarget(target);
        }

        /// <summary>引导模式下用第一个锚点替代castTarg骗过base的LOS检查。</summary>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            LocalTargetInfo actualTarget = castTarg;
            if (guidedActive && rawAnchors != null && rawAnchors.Count > 0)
            {
                // v7.0朝向优化：能直视最终目标 → 面朝目标（Cell形式避免Thing可见性检查）
                // 不能直视 → 面朝第一锚点（仅变化弹侧将发射）
                bool canSeeTarget = GenSight.LineOfSight(caster.Position, actualTarget.Cell, caster.Map);
                castTarg = canSeeTarget ? new LocalTargetInfo(actualTarget.Cell)
                                        : new LocalTargetInfo(rawAnchors[0]);
            }

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            if (result && guidedActive)
            {
                guidedSavedThingTarget = actualTarget;                      // 保存Thing供非变化弹侧
                currentTarget = new LocalTargetInfo(guidedTargetCell);      // 锁定Cell
            }

            return result;
        }

        /// <summary>侧别感知LOS检查：变化弹侧检查到第一锚点，非变化弹侧检查到最终目标。</summary>
        protected override LocalTargetInfo GetLosCheckTarget()
        {
            if (guidedActive && currentShotIsGuided && rawAnchors?.Count > 0)
                return new LocalTargetInfo(rawAnchors[0]);
            return base.GetLosCheckTarget();
        }

        /// <summary>弹道发射后回调：只有变化弹侧的子弹才附加引导路径。</summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (!guidedActive || !currentShotIsGuided || rawAnchors == null || rawAnchors.Count == 0)
                return;
            var waypoints = Verb_BDPGuided.BuildWaypoints(rawAnchors, rawFinalTarget, cachedAnchorSpread);
            if (waypoints.Count >= 2)
            {
                if (proj is Bullet_BDP bdp) bdp.InitGuidedFlight(waypoints);
                else if (proj is Projectile_ExplosiveBDP ebdp) ebdp.InitGuidedFlight(waypoints);
            }
        }

        /// <summary>
        /// 齐射TryCastShot：在单次调用中发射两侧所有子弹。
        /// v7.0变化弹：预判非变化弹侧是否有直视LOS，无LOS则跳过该侧整个循环。
        /// </summary>
        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn == null) return false;

            var triggerComp = pawn.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return false;

            // 读取两侧芯片数据
            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
            if (leftSlot?.loadedChip == null || rightSlot?.loadedChip == null)
                return false;

            var leftCfg = leftSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            if (leftCfg == null || rightCfg == null) return false;

            int leftCount = leftCfg.GetFirstBurstCount();
            int rightCount = rightCfg.GetFirstBurstCount();
            ThingDef leftProj = leftCfg.GetFirstProjectileDef();
            ThingDef rightProj = rightCfg.GetFirstProjectileDef();

            // v7.0：读取两侧引导标记（StartGuidedTargeting中已设置，此处兜底）
            if (guidedActive)
            {
                leftIsGuided = leftCfg.supportsGuided;
                rightIsGuided = rightCfg.supportsGuided;
            }

            // v7.1：用Thing的实时位置检查LOS（currentTarget已是固定Cell）
            bool hasDirectLos = !guidedActive || GenSight.LineOfSight(
                caster.Position,
                guidedSavedThingTarget.IsValid ? guidedSavedThingTarget.Cell : currentTarget.Cell,
                caster.Map);
            bool leftWillFire = !guidedActive || leftIsGuided || hasDirectLos;
            bool rightWillFire = !guidedActive || rightIsGuided || hasDirectLos;

            // 预检两侧Trion总消耗（跳过的侧不计费）
            float totalCost = (leftWillFire ? leftCount * leftCfg.trionCostPerShot : 0f)
                            + (rightWillFire ? rightCount * rightCfg.trionCostPerShot : 0f);
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost))
                return false;

            bool anyHit = false;
            ThingDef originalProjectile = verbProps.defaultProjectile;
            float spread = Mathf.Max(leftCfg.volleySpreadRadius, rightCfg.volleySpreadRadius);
            try
            {
                // 发射左侧所有子弹
                if (leftWillFire)
                {
                    currentShotIsGuided = guidedActive && leftIsGuided;
                    if (leftProj != null)
                        verbProps.defaultProjectile = leftProj;

                    // v7.1：非变化弹侧需要Thing跟踪目标
                    bool needThingRestoreL = guidedActive && !currentShotIsGuided && guidedSavedThingTarget.IsValid;
                    if (needThingRestoreL)
                        currentTarget = guidedSavedThingTarget;

                    for (int i = 0; i < leftCount; i++)
                    {
                        if (spread > 0f)
                            shotOriginOffset = new Vector3(
                                Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                        if (TryCastShotCore(leftSlot.loadedChip))
                            anyHit = true;
                    }

                    // 恢复为Cell（保持burst期间currentTarget稳定）
                    if (needThingRestoreL)
                        currentTarget = new LocalTargetInfo(guidedTargetCell);
                }

                // 发射右侧所有子弹
                if (rightWillFire)
                {
                    currentShotIsGuided = guidedActive && rightIsGuided;
                    if (rightProj != null)
                        verbProps.defaultProjectile = rightProj;

                    // v7.1：非变化弹侧需要Thing跟踪目标
                    bool needThingRestoreR = guidedActive && !currentShotIsGuided && guidedSavedThingTarget.IsValid;
                    if (needThingRestoreR)
                        currentTarget = guidedSavedThingTarget;

                    for (int i = 0; i < rightCount; i++)
                    {
                        if (spread > 0f)
                            shotOriginOffset = new Vector3(
                                Rand.Range(-spread, spread), 0f, Rand.Range(-spread, spread));
                        if (TryCastShotCore(rightSlot.loadedChip))
                            anyHit = true;
                    }

                    // 恢复为Cell
                    if (needThingRestoreR)
                        currentTarget = new LocalTargetInfo(guidedTargetCell);
                }
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
                shotOriginOffset = Vector3.zero;
                currentShotIsGuided = false;
            }

            // 一次性扣除Trion
            if (anyHit && totalCost > 0f)
                trion?.Consume(totalCost);

            return anyHit;
        }

    }
}