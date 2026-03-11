using BDP.Core;
using BDP.Trigger.ShotPipeline;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 双侧攻击Verb——v16.0管线重构版。
    /// 通过leftFiringPattern和rightFiringPattern字段区分每侧的发射模式。
    ///
    /// 发射模式组合：
    ///   · Sequential + Sequential：双侧逐发交替
    ///   · Simultaneous + Simultaneous：双侧齐射瞬发
    ///   · Sequential + Simultaneous：混合模式（一侧逐发，另一侧齐射）
    ///
    /// firingPattern由CompTriggerBody在创建Verb时从VerbChipConfig读取并设置。
    ///
    /// v16.0架构变化：
    ///   - 移除TryCastShot override，委托给管线系统
    ///   - ExecuteFire实现双侧交替逻辑
    ///   - LOS检查迁移到GetLosCheckTarget（基类已实现）
    /// </summary>
    public class Verb_BDPDual : Verb_BDPRangedBase
    {
        // ═══════════════════════════════════════════
        //  每侧发射模式（由CompTriggerBody在创建时设置）
        // ═══════════════════════════════════════════

        /// <summary>左侧发射模式。</summary>
        internal FiringPattern leftFiringPattern;

        /// <summary>右侧发射模式。</summary>
        internal FiringPattern rightFiringPattern;

        // ═══════════════════════════════════════════
        //  共享字段（从DualBase合并）
        // ═══════════════════════════════════════════

        protected int dualBurstIndex;
        protected int leftRemaining;
        protected int rightRemaining;
        protected ThingDef leftProjectileDef;
        protected ThingDef rightProjectileDef;
        protected bool leftSimultaneousFired;   // 原leftVolleyFired
        protected bool rightSimultaneousFired;  // 原rightVolleyFired

        // ── 双侧路径判断状态（从 VerbFlightState 迁移） ──
        private bool leftHasPath;
        private bool rightHasPath;
        private bool currentShotHasPath;
        private LocalTargetInfo savedThingTarget;

        // ═══════════════════════════════════════════
        //  Override方法（从DualBase合并）
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
            if (!(proj is BDP.Projectiles.Bullet_BDP bdp)) return;
            if (activeSession == null) return;

            // 双侧特殊逻辑：只有当前射击有路径时才注入引导数据
            if (activeSession.AimResult?.HasGuidedPath == true && currentShotHasPath)
            {
                bdp.InjectShotData(
                    activeSession.AimResult,
                    activeSession.FireResult,
                    activeSession.RouteResult);
            }
            else if (activeSession.RouteResult.HasValue)
            {
                // 自动绕行路径
                bdp.InjectShotData(
                    activeSession.AimResult,
                    activeSession.FireResult,
                    activeSession.RouteResult);
            }
        }

        protected override LocalTargetInfo GetLosCheckTarget()
        {
            // 双侧模式：只有当前射击有路径时才使用锚点
            if (activeSession?.AimResult?.HasGuidedPath == true && currentShotHasPath)
                return new LocalTargetInfo(activeSession.AimResult.AnchorPath[0]);
            return currentTarget;
        }

        public override void Reset()
        {
            base.Reset();
            dualBurstIndex = 0;
            leftRemaining = 0;
            rightRemaining = 0;
            leftProjectileDef = null;
            rightProjectileDef = null;
            leftSimultaneousFired = false;
            rightSimultaneousFired = false;
        }

        // ═══════════════════════════════════════════
        //  辅助方法（从DualBase合并）
        // ═══════════════════════════════════════════

        /// <summary>判断指定侧是否有引导路径。</summary>
        protected bool IsSideGuided(SlotSide side)
        {
            return side == SlotSide.LeftHand ? leftHasPath : rightHasPath;
        }

        /// <summary>在目标恢复上下文中执行action。</summary>
        protected void WithTargetRestore(System.Action action)
        {
            bool hasManualAnchors = activeSession?.AimResult?.HasGuidedPath == true;
            bool needRestore = hasManualAnchors && !currentShotHasPath && savedThingTarget.IsValid;
            LocalTargetInfo savedTarget = currentTarget;

            if (needRestore)
                currentTarget = savedThingTarget;

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

        /// <summary>在投射物def临时替换上下文中执行func。</summary>
        protected T WithProjectileSwap<T>(ThingDef sideProjectile, System.Func<T> func)
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

        // ═══════════════════════════════════════════
        //  双侧特定的TryStartCastOn（处理双侧初始化）
        // ═══════════════════════════════════════════

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
            leftSimultaneousFired = false;
            rightSimultaneousFired = false;

            // 双侧模式：保存Thing目标（用于非引导侧恢复）
            if (castTarg.HasThing)
                savedThingTarget = castTarg;

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            // 锁定currentTarget为Cell（防止Thing移动导致幽灵命中）
            if (result && activeSession?.AimResult != null)
                currentTarget = new LocalTargetInfo(activeSession.AimResult.FinalTarget.Cell);

            return result;
        }

        // ═══════════════════════════════════════════
        //  v16.0 ExecuteFire 双侧交替逻辑
        // ═══════════════════════════════════════════

        /// <summary>
        /// 执行双侧交替射击（v16.0管线重构）。
        /// 根据FiringPattern分发到对应的发射方法。
        /// </summary>
        protected override bool ExecuteFire(ShotSession session)
        {
            if (dualBurstIndex == 0)
            {
                InitDualBurst();
            }

            // 特殊路径：双侧都是齐射 → 一次性发射所有
            if (leftFiringPattern == FiringPattern.Simultaneous
                && rightFiringPattern == FiringPattern.Simultaneous)
            {
                // 记录攻击日志（在发射前，此时已应用FireMode）
                var triggerComp = GetTriggerComp();
                var leftSlot = triggerComp?.GetActiveSlot(SlotSide.LeftHand);
                var rightSlot = triggerComp?.GetActiveSlot(SlotSide.RightHand);

                if (leftSlot?.loadedChip != null && rightSlot?.loadedChip != null)
                {
                    var leftCfg = leftSlot.loadedChip.def.GetModExtension<VerbChipConfig>();
                    var rightCfg = rightSlot.loadedChip.def.GetModExtension<VerbChipConfig>();

                    // 获取FireMode调整后的子弹数量
                    int leftBulletCount = leftCfg?.GetPrimaryBurstCount() ?? 0;
                    int rightBulletCount = rightCfg?.GetPrimaryBurstCount() ?? 0;
                    var leftFm = GetFireMode(leftSlot.loadedChip);
                    var rightFm = GetFireMode(rightSlot.loadedChip);
                    if (leftFm != null) leftBulletCount = leftFm.GetEffectiveBurst(leftBulletCount);
                    if (rightFm != null) rightBulletCount = rightFm.GetEffectiveBurst(rightBulletCount);

                    VerbAttackLogger.LogDualAttack(
                        this,
                        leftSlot.loadedChip.def.defName,
                        rightSlot.loadedChip.def.defName,
                        leftFiringPattern,
                        rightFiringPattern,
                        leftBulletCount,
                        rightBulletCount,
                        verbProps.burstShotCount,
                        !verbProps.isPrimary
                    );
                }

                return FireBothSimultaneous();
            }

            // 通用路径日志记录（只在第一次记录）
            if (dualBurstIndex == 0)
            {
                var triggerComp = GetTriggerComp();
                var leftSlot = triggerComp?.GetActiveSlot(SlotSide.LeftHand);
                var rightSlot = triggerComp?.GetActiveSlot(SlotSide.RightHand);

                if (leftSlot?.loadedChip != null && rightSlot?.loadedChip != null)
                {
                    VerbAttackLogger.LogDualAttack(
                        this,
                        leftSlot.loadedChip.def.defName,
                        rightSlot.loadedChip.def.defName,
                        leftFiringPattern,
                        rightFiringPattern,
                        leftRemaining,
                        rightRemaining,
                        verbProps.burstShotCount,
                        !verbProps.isPrimary
                    );
                }
            }

            // 通用路径：至少一侧是逐发 → 按发射调度
            SlotSide shotSide = GetCurrentShotSide();
            bool hasManualAnchors = activeSession?.AimResult?.HasGuidedPath == true;
            currentShotHasPath = hasManualAnchors && IsSideGuided(shotSide);

            bool isSideSimultaneous =
                (shotSide == SlotSide.LeftHand && leftFiringPattern == FiringPattern.Simultaneous)
             || (shotSide == SlotSide.RightHand && rightFiringPattern == FiringPattern.Simultaneous);

            if (isSideSimultaneous)
                return FireSideSimultaneous(shotSide);
            else
                return FireSideSequential(shotSide);
        }

        // ═══════════════════════════════════════════
        //  发射方法（合并自三个子类）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 双侧齐射：一次性发射两侧所有子弹。
        /// </summary>
        private bool FireBothSimultaneous()
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
            var leftFm = GetFireMode(leftSlot.loadedChip);
            var rightFm = GetFireMode(rightSlot.loadedChip);
            if (leftFm != null) leftCount = leftFm.GetEffectiveBurst(leftCount);
            if (rightFm != null) rightCount = rightFm.GetEffectiveBurst(rightCount);

            // 引导模块支持：标记哪一侧有引导路径
            bool hasManualAnchors = activeSession?.AimResult?.HasGuidedPath == true;
            if (hasManualAnchors)
            {
                leftHasPath = leftCfg.ranged?.guided != null;
                rightHasPath = rightCfg.ranged?.guided != null;
            }

            // 架构修正：降级逻辑——有引导路径或有直视LOS才能发射
            bool hasDirectLos = GenSight.LineOfSight(caster.Position,
                savedThingTarget.IsValid ? savedThingTarget.Cell : currentTarget.Cell, caster.Map);
            bool leftWillFire = leftHasPath || hasDirectLos;
            bool rightWillFire = rightHasPath || hasDirectLos;

            // 获取使用消耗（统一层）
            float leftUsageCost = ChipUsageCostHelper.GetUsageCost(leftSlot.loadedChip);
            float rightUsageCost = ChipUsageCostHelper.GetUsageCost(rightSlot.loadedChip);
            float totalCost = (leftWillFire ? leftUsageCost : 0f) + (rightWillFire ? rightUsageCost : 0f);
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost)) return false;

            bool anyHit = false;
            ThingDef originalProjectile = verbProps.defaultProjectile;
            float spread = Mathf.Max(leftCfg.ranged?.volleySpreadRadius ?? 0f, rightCfg.ranged?.volleySpreadRadius ?? 0f);

            try
            {
                // 左侧齐射
                if (leftWillFire)
                {
                    currentShotHasPath = hasManualAnchors && leftHasPath;
                    if (leftProjectileDef != null) verbProps.defaultProjectile = leftProjectileDef;
                    bool needRestoreL = hasManualAnchors && !currentShotHasPath && savedThingTarget.IsValid;
                    if (needRestoreL) currentTarget = savedThingTarget;

                    if (FireVolleyLoop(leftCount, spread, leftSlot.loadedChip))
                        anyHit = true;

                    if (needRestoreL && activeSession?.AimResult != null)
                        currentTarget = new LocalTargetInfo(activeSession.AimResult.FinalTarget.Cell);
                }

                // 右侧齐射
                if (rightWillFire)
                {
                    currentShotHasPath = hasManualAnchors && rightHasPath;
                    if (rightProjectileDef != null) verbProps.defaultProjectile = rightProjectileDef;
                    bool needRestoreR = hasManualAnchors && !currentShotHasPath && savedThingTarget.IsValid;
                    if (needRestoreR) currentTarget = savedThingTarget;

                    if (FireVolleyLoop(rightCount, spread, rightSlot.loadedChip))
                        anyHit = true;

                    if (needRestoreR && activeSession?.AimResult != null)
                        currentTarget = new LocalTargetInfo(activeSession.AimResult.FinalTarget.Cell);
                }
            }
            finally
            {
                verbProps.defaultProjectile = originalProjectile;
                shotOriginOffset = Vector3.zero;
                currentShotHasPath = false;
            }

            if (anyHit && totalCost > 0f) trion?.Consume(totalCost);
            return anyHit;
        }

        /// <summary>
        /// 单侧齐射：瞬发该侧所有子弹。
        /// </summary>
        private bool FireSideSimultaneous(SlotSide side)
        {
            var triggerComp = GetTriggerComp();
            var pawn = CasterPawn;
            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) { AdvanceBurstIndex(); return false; }
            var cfg = slot.loadedChip.def.GetModExtension<VerbChipConfig>();
            if (cfg == null) { AdvanceBurstIndex(); return false; }

            int volleyCount = cfg.GetPrimaryBurstCount();
            var fm = GetFireMode(slot.loadedChip);
            if (fm != null) volleyCount = fm.GetEffectiveBurst(volleyCount);

            // 获取使用消耗（统一层）
            float usageCost = ChipUsageCostHelper.GetUsageCost(slot.loadedChip);
            var trion = pawn?.GetComp<CompTrion>();
            if (usageCost > 0f && (trion == null || trion.Available < usageCost))
            {
                AdvanceBurstIndex();
                return false;
            }

            ThingDef sideProjectile = side == SlotSide.LeftHand ? leftProjectileDef : rightProjectileDef;
            float spread = cfg.ranged?.volleySpreadRadius ?? 0f;

            bool anyHit = false;
            WithTargetRestore(() =>
            {
                anyHit = WithProjectileSwap(sideProjectile, () =>
                    FireVolleyLoop(volleyCount, spread, slot.loadedChip));
            });

            // 扣除Trion（统一层）
            if (anyHit && usageCost > 0f) trion?.Consume(usageCost);

            // 标记齐射侧已发射
            if (side == SlotSide.LeftHand) leftSimultaneousFired = true;
            else rightSimultaneousFired = true;

            AdvanceBurstIndex();
            return anyHit;
        }

        /// <summary>
        /// 单侧逐发：发射该侧一颗子弹。
        /// </summary>
        private bool FireSideSequential(SlotSide side)
        {
            var triggerComp = GetTriggerComp();
            var pawn = CasterPawn;
            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) { AdvanceBurstIndex(); return false; }

            float cost = ChipUsageCostHelper.GetUsageCost(slot.loadedChip);
            if (cost > 0f)
            {
                var trion = pawn?.GetComp<CompTrion>();
                if (trion == null || trion.Available < cost)
                {
                    AdvanceBurstIndex();
                    return false;
                }
                trion.Consume(cost);
            }

            Thing chipEquipment = slot.loadedChip;
            ThingDef sideProjectile = side == SlotSide.LeftHand ? leftProjectileDef : rightProjectileDef;

            bool result = false;
            WithTargetRestore(() =>
            {
                result = WithProjectileSwap(sideProjectile, () => TryCastShotCore(chipEquipment));
            });

            if (side == SlotSide.LeftHand) leftRemaining--;
            else rightRemaining--;

            AdvanceBurstIndex();
            return result;
        }

        private void AdvanceBurstIndex()
        {
            dualBurstIndex++;
            if (burstShotsLeft <= 1) dualBurstIndex = 0;
        }

        // ═══════════════════════════════════════════
        //  初始化和调度（合并自三个子类）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 初始化双侧burst状态。
        /// 合并自DualRanged和DualMixed的InitDualBurst，统一用FiringPattern替代IsVolleyChip。
        /// </summary>
        private void InitDualBurst()
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null)
            {
                leftRemaining = verbProps.burstShotCount;
                rightRemaining = 0;
                leftProjectileDef = null;
                rightProjectileDef = null;
                leftHasPath = false;
                rightHasPath = false;
                return;
            }

            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
            var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

            // 齐射侧算1发（引擎层面只调用一次TryCastShot），逐发侧算实际发数
            leftRemaining = leftFiringPattern == FiringPattern.Simultaneous
                ? 1 : (leftCfg?.GetPrimaryBurstCount() ?? 0);
            rightRemaining = rightFiringPattern == FiringPattern.Simultaneous
                ? 1 : (rightCfg?.GetPrimaryBurstCount() ?? 0);

            // FireMode注入（只对逐发侧有效）
            if (leftFiringPattern == FiringPattern.Sequential)
            {
                var leftFm = GetFireMode(leftSlot?.loadedChip);
                if (leftFm != null) leftRemaining = leftFm.GetEffectiveBurst(leftRemaining);
            }
            if (rightFiringPattern == FiringPattern.Sequential)
            {
                var rightFm = GetFireMode(rightSlot?.loadedChip);
                if (rightFm != null) rightRemaining = rightFm.GetEffectiveBurst(rightRemaining);
            }

            // 架构修正：在初始化时检查每一侧的LOS
            IntVec3 finalTargetCell = savedThingTarget.IsValid
                ? savedThingTarget.Cell
                : currentTarget.Cell;

            // 左侧LOS检查：如果不支持引导且无直视LOS，排除这一侧
            if (leftCfg != null && leftRemaining > 0 && leftCfg.ranged?.guided == null)
            {
                if (!GenSight.LineOfSight(caster.Position, finalTargetCell, caster.Map))
                    leftRemaining = 0;
            }

            // 右侧LOS检查：如果不支持引导且无直视LOS，排除这一侧
            if (rightCfg != null && rightRemaining > 0 && rightCfg.ranged?.guided == null)
            {
                if (!GenSight.LineOfSight(caster.Position, finalTargetCell, caster.Map))
                    rightRemaining = 0;
            }

            // 注意：不再在运行时修改verbProps.burstShotCount，应该在创建时就设置正确
            // verbProps.burstShotCount已经在DualVerbCompositor和CompTriggerBody中正确设置

            leftProjectileDef = leftCfg?.GetPrimaryProjectileDef();
            rightProjectileDef = rightCfg?.GetPrimaryProjectileDef();
            leftHasPath = leftCfg?.ranged?.guided != null;
            rightHasPath = rightCfg?.ranged?.guided != null;

            leftSimultaneousFired = false;
            rightSimultaneousFired = false;
        }

        /// <summary>
        /// 确定当前发应使用哪一侧。
        /// 合并自DualRanged和DualMixed的GetCurrentShotSide。
        /// </summary>
        private SlotSide GetCurrentShotSide()
        {
            if (leftRemaining <= 0 && rightRemaining <= 0)
                return SlotSide.LeftHand;

            // 齐射侧只发射一次
            if (leftFiringPattern == FiringPattern.Simultaneous && leftSimultaneousFired)
                leftRemaining = 0;
            if (rightFiringPattern == FiringPattern.Simultaneous && rightSimultaneousFired)
                rightRemaining = 0;

            if (leftRemaining > 0 && rightRemaining > 0)
            {
                // 交替发射
                if (dualBurstIndex % 2 == 0)
                    return SlotSide.LeftHand;
                else
                    return SlotSide.RightHand;
            }
            else if (leftRemaining > 0)
                return SlotSide.LeftHand;
            else
                return SlotSide.RightHand;
        }
    }
}






