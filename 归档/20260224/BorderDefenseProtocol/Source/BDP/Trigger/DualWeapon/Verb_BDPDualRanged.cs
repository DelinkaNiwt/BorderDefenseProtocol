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
    /// 射程：min(左, 右)  瞄准/冷却：max(左, 右)
    ///
    /// v7.0变化弹适配：任一侧supportsGuided=true时，双手触发走锚点瞄准逻辑。
    ///   确认目标后：变化弹侧走锚点折线，非变化弹侧直射（无LOS则跳过）。
    /// </summary>
    public class Verb_BDPDualRanged : Verb_BDPRangedBase
    {
        // 当前burst中的发射序号（用于交替子弹类型）
        private int dualBurstIndex = 0;

        // 两侧的剩余发射数（每次burst开始时重置）
        private int leftRemaining = 0;
        private int rightRemaining = 0;

        // B4修复：缓存两侧的projectileDef，用于per-shot子弹类型切换
        private ThingDef leftProjectileDef;
        private ThingDef rightProjectileDef;

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
        /// <summary>当前发射的子弹是否属于变化弹侧（per-shot标记，供GetLosCheckTarget和OnProjectileLaunched使用）。</summary>
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
        /// 启动多步锚点瞄准（由Command_BDPChipAttack.GizmoOnGUIInt调用）。
        /// 取第一个supportsGuided=true侧的maxAnchors/anchorSpread配置。
        /// </summary>
        public void StartGuidedTargeting()
        {
            var pawn = CasterPawn;
            var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) { Find.Targeter.BeginTargeting(this); return; }

            // 找到第一个supportsGuided=true的芯片配置
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
                    // 调用基类创建Job发射（目标为finalTarget）
                    base.OrderForceTarget(finalTarget);
                });
        }

        /// <summary>
        /// OrderForceTarget：引导模式下拦截，启动锚点瞄准（兼容非Gizmo调用路径）。
        /// </summary>
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

        /// <summary>
        /// 引导模式下用第一个锚点替代castTarg骗过base的LOS检查。
        /// </summary>
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

            // 恢复currentTarget：保存Thing供非变化弹侧，锁定Cell防转身
            if (result && guidedActive)
            {
                guidedSavedThingTarget = actualTarget;                      // 保存Thing供非变化弹侧
                currentTarget = new LocalTargetInfo(guidedTargetCell);      // 锁定Cell
            }

            return result;
        }

        /// <summary>
        /// 侧别感知LOS检查：变化弹侧检查到第一锚点，非变化弹侧检查到最终目标。
        /// </summary>
        protected override LocalTargetInfo GetLosCheckTarget()
        {
            if (guidedActive && currentShotIsGuided && rawAnchors?.Count > 0)
                return new LocalTargetInfo(rawAnchors[0]);
            return base.GetLosCheckTarget();
        }

        /// <summary>
        /// 弹道发射后回调：只有变化弹侧的子弹才附加引导路径。
        /// </summary>
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
        /// 重写TryCastShot：根据当前burst序号选择对应侧的子弹类型。
        /// 交替规则：偶数发用左侧，奇数发用右侧，一方用完后全部用另一方。
        /// 每发射击前按侧读取trionCostPerShot并Consume。
        /// v7.0变化弹：确定shotSide后设置currentShotIsGuided，非变化弹侧无LOS时跳过。
        /// </summary>
        protected override bool TryCastShot()
        {
            // 首发时初始化交替状态
            if (dualBurstIndex == 0)
                InitDualBurst();

            // 确定当前发使用哪一侧，并消耗Trion
            SlotSide shotSide = GetCurrentShotSide();

            // v7.0：设置per-shot引导标记
            currentShotIsGuided = guidedActive && IsSideGuided(shotSide);

            // 引导模式下，非变化弹侧无直视LOS → 跳过这发（不消耗Trion，不中断burst）
            // 用guidedSavedThingTarget的实时位置检查（currentTarget已是固定Cell）
            if (guidedActive && !currentShotIsGuided)
            {
                IntVec3 losCell = guidedSavedThingTarget.IsValid ? guidedSavedThingTarget.Cell : currentTarget.Cell;
                if (!GenSight.LineOfSight(caster.Position, losCell, caster.Map))
                {
                    dualBurstIndex++;
                    if (burstShotsLeft <= 1) dualBurstIndex = 0;
                    return true; // 返回true保持burst继续
                }
            }

            float cost = GetSideTrionCost(shotSide);
            if (cost > 0f)
            {
                var trion = CasterPawn?.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost)
                    return false; // Trion不足，中止射击
                trion.Consume(cost);
            }

            // B3修复：使用当前侧芯片Thing作为equipment source
            Thing chipEquipment = GetSideChipThing(shotSide);

            // v7.1：非变化弹侧需要Thing跟踪目标，临时恢复后还原Cell
            bool needThingRestore = guidedActive && !currentShotIsGuided && guidedSavedThingTarget.IsValid;
            if (needThingRestore)
                currentTarget = guidedSavedThingTarget;

            // B4修复：根据侧别切换projectileDef
            ThingDef originalProjectile = verbProps.defaultProjectile;
            ThingDef sideProjectile = shotSide == SlotSide.LeftHand ? leftProjectileDef : rightProjectileDef;
            bool result;
            try
            {
                if (sideProjectile != null)
                    verbProps.defaultProjectile = sideProjectile;

                result = TryCastShotCore(chipEquipment);
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
            }

            // 恢复为Cell（保持burst期间currentTarget稳定）
            if (needThingRestore)
                currentTarget = new LocalTargetInfo(guidedTargetCell);

            dualBurstIndex++;

            // burst结束时重置
            if (burstShotsLeft <= 1)
                dualBurstIndex = 0;

            return result;
        }

        /// <summary>判断指定侧是否为变化弹。</summary>
        private bool IsSideGuided(SlotSide side)
        {
            return side == SlotSide.LeftHand ? leftIsGuided : rightIsGuided;
        }

        /// <summary>初始化双射burst状态：从CompTriggerBody读取两侧连射数和projectileDef。</summary>
        private void InitDualBurst()
        {
            var pawn = CasterPawn;
            var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null)
            {
                leftRemaining = verbProps.burstShotCount;
                rightRemaining = 0;
                leftProjectileDef = null;
                rightProjectileDef = null;
                leftIsGuided = false;
                rightIsGuided = false;
                return;
            }

            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);

            var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();

            leftRemaining = leftCfg?.GetFirstBurstCount() ?? 0;
            rightRemaining = rightCfg?.GetFirstBurstCount() ?? 0;

            leftProjectileDef = leftCfg?.GetFirstProjectileDef();
            rightProjectileDef = rightCfg?.GetFirstProjectileDef();

            // v7.0：读取两侧引导标记
            leftIsGuided = leftCfg?.supportsGuided == true;
            rightIsGuided = rightCfg?.supportsGuided == true;
        }

        /// <summary>
        /// 确定当前发应使用哪一侧。
        /// 交替规则：偶数发左侧，奇数发右侧，一方用完后全部用另一方。
        /// </summary>
        private SlotSide GetCurrentShotSide()
        {
            if (leftRemaining > 0 && rightRemaining > 0)
            {
                if (dualBurstIndex % 2 == 0)
                {
                    leftRemaining--;
                    return SlotSide.LeftHand;
                }
                else
                {
                    rightRemaining--;
                    return SlotSide.RightHand;
                }
            }
            else if (leftRemaining > 0)
            {
                leftRemaining--;
                return SlotSide.LeftHand;
            }
            else
            {
                rightRemaining--;
                return SlotSide.RightHand;
            }
        }

        /// <summary>获取指定侧芯片的trionCostPerShot。</summary>
        private float GetSideTrionCost(SlotSide side)
        {
            var pawn = CasterPawn;
            var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return 0f;

            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) return 0f;

            var cfg = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            return cfg?.trionCostPerShot ?? 0f;
        }

        /// <summary>获取指定侧芯片的Thing实例（B3：供TryCastShotCore使用）。</summary>
        private Thing GetSideChipThing(SlotSide side)
        {
            var pawn = CasterPawn;
            var triggerComp = pawn?.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            return triggerComp?.GetActiveSlot(side)?.loadedChip;
        }
    }
}