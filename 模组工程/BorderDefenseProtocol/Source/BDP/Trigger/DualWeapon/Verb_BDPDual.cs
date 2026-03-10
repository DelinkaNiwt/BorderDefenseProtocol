using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 双侧攻击Verb——v15.0合并四个双武器类为统一实现。
    /// 通过leftFiringPattern和rightFiringPattern字段区分每侧的发射模式。
    ///
    /// 发射模式组合：
    ///   · Sequential + Sequential：双侧逐发交替
    ///   · Simultaneous + Simultaneous：双侧齐射瞬发
    ///   · Sequential + Simultaneous：混合模式（一侧逐发，另一侧齐射）
    ///   · Sequential + Simultaneous：混合模式（原DualMixed）
    ///
    /// firingPattern由CompTriggerBody在创建Verb时从VerbChipConfig读取并设置。
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
            return side == SlotSide.LeftHand ? gs.LeftHasPath : gs.RightHasPath;
        }

        /// <summary>在目标恢复上下文中执行action。</summary>
        protected void WithTargetRestore(System.Action action)
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
        //  统一的TryStartCastOn（合并自三个子类）
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

            gs.ResetAutoRouteCastState();
            LocalTargetInfo actualTarget = gs.InterceptDualCastTarget(
                ref castTarg, caster.Position, caster.Map);

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            if (result)
                gs.PostDualCastOn(ref currentTarget, actualTarget);

            return result;
        }

        // ═══════════════════════════════════════════
        //  统一的TryCastShot（根据FiringPattern分发）
        // ═══════════════════════════════════════════

        protected override bool TryCastShot()
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
            gs.CurrentShotHasPath = gs.ManualAnchorsActive && IsSideGuided(shotSide);

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
            if (gs.ManualAnchorsActive)
            {
                gs.LeftHasPath = leftCfg.ranged?.guided != null;
                gs.RightHasPath = rightCfg.ranged?.guided != null;
            }

            // 架构修正：降级逻辑——有引导路径或有直视LOS才能发射
            // 原逻辑错误：!gs.ManualAnchorsActive会导致无引导且无LOS的一侧也发射
            // 正确逻辑：有引导路径(gs.LeftHasPath/RightHasPath) 或 有直视LOS
            bool hasDirectLos = GenSight.LineOfSight(caster.Position,
                gs.SavedThingTarget.IsValid ? gs.SavedThingTarget.Cell : currentTarget.Cell, caster.Map);
            bool leftWillFire = gs.LeftHasPath || hasDirectLos;
            bool rightWillFire = gs.RightHasPath || hasDirectLos;

            // 获取使用消耗（统一层）
            float leftUsageCost = ChipUsageCostHelper.GetUsageCost(leftSlot.loadedChip);
            float rightUsageCost = ChipUsageCostHelper.GetUsageCost(rightSlot.loadedChip);
            float totalCost = (leftWillFire ? leftUsageCost : 0f) + (rightWillFire ? rightUsageCost : 0f);
            var trion = pawn.GetComp<CompTrion>();
            if (totalCost > 0f && (trion == null || trion.Available < totalCost)) return false;

            bool anyHit = false;
            ThingDef originalProjectile = verbProps.defaultProjectile;
            float spread = Mathf.Max(leftCfg.ranged?.volleySpreadRadius ?? 0f, rightCfg.ranged?.volleySpreadRadius ?? 0f);

            gs.PrepareAutoRoute(caster.Position, currentTarget.Cell, caster.Map, leftProjectileDef);

            try
            {
                // 左侧齐射
                if (leftWillFire)
                {
                    gs.CurrentShotHasPath = gs.ManualAnchorsActive && gs.LeftHasPath;
                    if (leftProjectileDef != null) verbProps.defaultProjectile = leftProjectileDef;
                    bool needRestoreL = gs.ManualAnchorsActive && !gs.CurrentShotHasPath && gs.SavedThingTarget.IsValid;
                    if (needRestoreL) currentTarget = gs.SavedThingTarget;

                    if (FireVolleyLoop(leftCount, spread, leftSlot.loadedChip))
                        anyHit = true;

                    if (needRestoreL) currentTarget = new LocalTargetInfo(gs.ManualTargetCell);
                }

                // 右侧齐射
                if (rightWillFire)
                {
                    gs.CurrentShotHasPath = gs.ManualAnchorsActive && gs.RightHasPath;
                    if (rightProjectileDef != null) verbProps.defaultProjectile = rightProjectileDef;
                    bool needRestoreR = gs.ManualAnchorsActive && !gs.CurrentShotHasPath && gs.SavedThingTarget.IsValid;
                    if (needRestoreR) currentTarget = gs.SavedThingTarget;

                    if (FireVolleyLoop(rightCount, spread, rightSlot.loadedChip))
                        anyHit = true;

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
                gs.LeftHasPath = false;
                gs.RightHasPath = false;
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
            IntVec3 finalTargetCell = gs.SavedThingTarget.IsValid
                ? gs.SavedThingTarget.Cell
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
            gs.LeftHasPath = leftCfg?.ranged?.guided != null;
            gs.RightHasPath = rightCfg?.ranged?.guided != null;

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

        // ── 范围指示器支持 ──

        /// <summary>
        /// 绘制双侧范围指示器。
        /// 分别读取左右两侧芯片的配置，在目标位置绘制影响范围。
        /// 两个圆圈使用相同颜色，重叠部分透明度自然叠加。
        /// </summary>
        public override void DrawAreaIndicators(LocalTargetInfo target)
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return;

            // 左侧范围
            var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
            if (leftSlot?.loadedChip != null)
            {
                var leftChipConfig = leftSlot.loadedChip.def.GetModExtension<VerbChipConfig>();
                var leftProjectileDef = leftChipConfig?.GetPrimaryProjectileDef();
                DrawSideIndicator(leftProjectileDef, target);
            }

            // 右侧范围
            var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
            if (rightSlot?.loadedChip != null)
            {
                var rightChipConfig = rightSlot.loadedChip.def.GetModExtension<VerbChipConfig>();
                var rightProjectileDef = rightChipConfig?.GetPrimaryProjectileDef();
                DrawSideIndicator(rightProjectileDef, target);
            }
        }

        /// <summary>
        /// 绘制单侧范围指示器（辅助方法）。
        /// </summary>
        private void DrawSideIndicator(ThingDef projectileDef, LocalTargetInfo target)
        {
            if (projectileDef == null) return;

            var indicatorConfig = GetAreaIndicatorConfig(projectileDef);
            if (indicatorConfig == null) return;

            // 创建临时配置（计算实际半径）
            var tempConfig = new AreaIndicatorConfig
            {
                indicatorType = indicatorConfig.indicatorType,
                radiusSource = indicatorConfig.radiusSource,
                customRadius = GetIndicatorRadius(projectileDef, indicatorConfig),
                color = indicatorConfig.color,
                fillStyle = indicatorConfig.fillStyle
            };

            // 绘制圆形指示器
            var indicator = new CircleAreaIndicator();
            indicator.Draw(target.Cell, caster.Map, tempConfig);
        }
    }
}






