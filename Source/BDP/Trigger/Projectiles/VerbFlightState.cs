using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Verb飞行状态——管理锚点数据、LOS检查重定向、TryStartCastOn拦截、弹道附加。
    ///
    /// PMS重构：由Verb_BDPRangedBase基类持有，所有远程Verb共用。
    /// v5命名修正：消除"Guided"泛化命名，改用语义精确的名称。
    /// </summary>
    public class VerbFlightState
    {
        // ── 共享状态 ──
        /// <summary>锚点原始坐标（未散布）。</summary>
        public List<IntVec3> RawAnchors;
        /// <summary>最终目标。</summary>
        public LocalTargetInfo RawFinalTarget;
        /// <summary>芯片散布半径缓存。</summary>
        public float CachedAnchorSpread;
        /// <summary>是否处于手动锚点模式。</summary>
        public bool ManualAnchorsActive;
        /// <summary>瞄准确认时快照的目标地格（不随Thing移动）。</summary>
        public IntVec3 ManualTargetCell;

        // ── 双侧专用（单侧Verb不使用） ──
        /// <summary>原始Thing目标引用（供非变化弹侧跟踪）。</summary>
        public LocalTargetInfo SavedThingTarget;
        /// <summary>左侧是否有路径（变化弹）。</summary>
        public bool LeftHasPath;
        /// <summary>右侧是否有路径（变化弹）。</summary>
        public bool RightHasPath;
        /// <summary>当前发射的子弹是否有路径（属于变化弹侧）。</summary>
        public bool CurrentShotHasPath;

        // ── 自动绕行路由（ObstacleRouter） ──
        /// <summary>每次齐射缓存的绕行路由结果。</summary>
        private ObstacleRouteResult? cachedRoute;
        /// <summary>交替分配左/右路径的计数器。</summary>
        private int autoRouteIndex;
        /// <summary>本次施法是否已准备自动绕行首锚点（供TryStartCastOn/LOS检查使用）。</summary>
        private bool autoRouteCastPrepared;
        /// <summary>自动绕行施法的原始最终目标（用于PostCastOn恢复currentTarget）。</summary>
        private LocalTargetInfo autoRouteFinalTarget;
        /// <summary>自动绕行施法的首锚点（用于替代不可视最终目标做LOS检查）。</summary>
        private IntVec3 autoRouteLosCell;

        /// <summary>存储锚点瞄准结果。</summary>
        public void StoreTargetingResult(
            List<IntVec3> anchors, LocalTargetInfo finalTarget, float spread)
        {
            RawAnchors = new List<IntVec3>(anchors);
            RawFinalTarget = finalTarget;
            CachedAnchorSpread = spread;
            ManualTargetCell = finalTarget.Cell;
            ManualAnchorsActive = anchors.Count > 0;
            autoRouteCastPrepared = false;
        }

        /// <summary>获取LOS检查目标（单侧模式：引导时返回第一锚点）。</summary>
        public LocalTargetInfo GetLosCheckTarget(LocalTargetInfo defaultTarget)
        {
            if (ManualAnchorsActive && RawAnchors?.Count > 0)
                return new LocalTargetInfo(RawAnchors[0]);
            if (autoRouteCastPrepared)
                return new LocalTargetInfo(autoRouteLosCell);
            return defaultTarget;
        }

        /// <summary>获取LOS检查目标（双侧模式：感知CurrentShotHasPath）。</summary>
        public LocalTargetInfo GetDualLosCheckTarget(LocalTargetInfo defaultTarget)
        {
            if (ManualAnchorsActive && CurrentShotHasPath && RawAnchors?.Count > 0)
                return new LocalTargetInfo(RawAnchors[0]);
            if (autoRouteCastPrepared)
                return new LocalTargetInfo(autoRouteLosCell);
            return defaultTarget;
        }

        /// <summary>
        /// TryStartCastOn前处理（单侧模式）：根据LOS决定朝向。
        /// 能直视目标→保持朝向目标；不能直视→朝向第一锚点。
        /// 返回实际最终目标（调用方需保存）。
        /// </summary>
        public LocalTargetInfo InterceptCastTarget(
            ref LocalTargetInfo castTarg, IntVec3 casterPos, Map map)
        {
            LocalTargetInfo actualTarget = castTarg;
            if (ManualAnchorsActive && RawAnchors != null && RawAnchors.Count > 0)
            {
                // 能直视目标时保持朝向目标，否则朝向第一锚点。
                // skipFirstCell=true：与引擎CanHitCellFromCellIgnoringRange保持一致，
                // 避免施法者自身格子影响判断。
                bool canSeeTarget = GenSight.LineOfSight(casterPos, actualTarget.Cell, map, skipFirstCell: true);
                if (!canSeeTarget)
                    castTarg = new LocalTargetInfo(RawAnchors[0]);
            }
            else if (autoRouteCastPrepared)
            {
                castTarg = new LocalTargetInfo(autoRouteLosCell);
            }
            return actualTarget;
        }

        /// <summary>
        /// TryStartCastOn前处理（双侧模式）：根据LOS选择面朝方向。
        /// 返回实际最终目标（调用方需保存）。
        /// </summary>
        public LocalTargetInfo InterceptDualCastTarget(
            ref LocalTargetInfo castTarg, IntVec3 casterPos, Map map)
        {
            LocalTargetInfo actualTarget = castTarg;
            if (ManualAnchorsActive && RawAnchors != null && RawAnchors.Count > 0)
            {
                bool canSeeTarget = GenSight.LineOfSight(casterPos, actualTarget.Cell, map, skipFirstCell: true);
                castTarg = canSeeTarget ? new LocalTargetInfo(actualTarget.Cell)
                                        : new LocalTargetInfo(RawAnchors[0]);
            }
            else if (autoRouteCastPrepared)
            {
                castTarg = new LocalTargetInfo(autoRouteLosCell);
            }
            return actualTarget;
        }

        /// <summary>TryStartCastOn后处理（单侧模式）：锁定currentTarget为Cell。</summary>
        public void PostCastOn(ref LocalTargetInfo currentTarget)
        {
            if (ManualAnchorsActive)
                currentTarget = new LocalTargetInfo(ManualTargetCell);
            else if (autoRouteCastPrepared && autoRouteFinalTarget.IsValid)
                currentTarget = new LocalTargetInfo(autoRouteFinalTarget.Cell);
        }

        /// <summary>TryStartCastOn后处理（双侧模式）：保存Thing并锁定Cell。</summary>
        public void PostDualCastOn(
            ref LocalTargetInfo currentTarget, LocalTargetInfo actualTarget)
        {
            if (ManualAnchorsActive)
            {
                SavedThingTarget = actualTarget;
                currentTarget = new LocalTargetInfo(ManualTargetCell);
            }
            else if (autoRouteCastPrepared && autoRouteFinalTarget.IsValid)
            {
                SavedThingTarget = autoRouteFinalTarget;
                currentTarget = new LocalTargetInfo(autoRouteFinalTarget.Cell);
            }
        }

        /// <summary>
        /// 自动绕行模式下返回真实最终目标（可能为Thing），否则回退传入目标。
        /// 用于在保持currentTarget为Cell防止幽灵命中的同时，给模块传入真实追踪目标。
        /// </summary>
        public LocalTargetInfo ResolveAutoRouteFinalTarget(LocalTargetInfo fallback)
            => (autoRouteCastPrepared && autoRouteFinalTarget.IsValid) ? autoRouteFinalTarget : fallback;

        /// <summary>为弹道附加手动锚点路径（通用）。</summary>
        public void AttachManualFlight(Projectile proj)
        {
            if (!ManualAnchorsActive || RawAnchors == null || RawAnchors.Count == 0)
                return;
            // v5解耦：通过宿主API注入引导路径，不直接碰模块
            if (proj is Bullet_BDP bdp)
            {
                bdp.TryInitGuidedFlight(RawAnchors, RawFinalTarget, CachedAnchorSpread);
            }
        }

        /// <summary>为弹道附加手动锚点路径（仅当CurrentShotHasPath时）。</summary>
        public void AttachManualFlightIfActive(Projectile proj)
        {
            if (!ManualAnchorsActive || !CurrentShotHasPath)
                return;
            AttachManualFlight(proj);
        }

        // ── 自动绕行路由 ──

        /// <summary>
        /// TryStartCastOn前调用：为"无手动锚点+不可视最终目标"准备自动绕行施法上下文。
        /// 成功时会给出首锚点用于LOS检查，同时保持最终目标不变。
        /// </summary>
        public void PrepareAutoRouteForCast(IntVec3 shooterPos, LocalTargetInfo finalTarget,
            Map map, ThingDef projectileDef)
        {
            autoRouteCastPrepared = false;
            autoRouteFinalTarget = finalTarget;
            autoRouteLosCell = default;

            if (!finalTarget.IsValid) return;

            PrepareAutoRoute(shooterPos, finalTarget.Cell, map, projectileDef);
            if (cachedRoute == null || !cachedRoute.Value.IsValid) return;

            if (!TryPickLosAnchor(cachedRoute.Value, shooterPos, out IntVec3 losAnchor))
                return;

            autoRouteLosCell = losAnchor;
            autoRouteCastPrepared = true;
        }

        /// <summary>为自动绕行施法选择一个首锚点（两侧都可用时选更近的一侧）。</summary>
        private static bool TryPickLosAnchor(ObstacleRouteResult route, IntVec3 shooterPos, out IntVec3 anchor)
        {
            anchor = default;
            bool hasLeft = route.LeftAnchors != null && route.LeftAnchors.Count > 0;
            bool hasRight = route.RightAnchors != null && route.RightAnchors.Count > 0;
            if (!hasLeft && !hasRight) return false;

            if (hasLeft && hasRight)
            {
                IntVec3 left = route.LeftAnchors[0];
                IntVec3 right = route.RightAnchors[0];
                int leftDist = (left - shooterPos).LengthHorizontalSquared;
                int rightDist = (right - shooterPos).LengthHorizontalSquared;
                anchor = leftDist <= rightDist ? left : right;
                return true;
            }

            anchor = hasLeft ? route.LeftAnchors[0] : route.RightAnchors[0];
            return true;
        }

        /// <summary>
        /// 齐射前调用一次，检测障碍物并缓存绕行路由。
        /// 触发条件：ManualAnchorsActive==false（无手动锚点）、弹药有BDPGuidedConfig、无LOS。
        /// </summary>
        public void PrepareAutoRoute(IntVec3 shooterPos, IntVec3 targetPos,
            Map map, ThingDef projectileDef)
        {
            cachedRoute = null;
            autoRouteIndex = 0;

            // 条件3：玩家已手动设锚点，不自动绕行
            if (ManualAnchorsActive) return;
            // 条件1：弹药必须有GuidedModule标记
            if (projectileDef?.GetModExtension<BDPGuidedConfig>() == null) return;
            // 条件4：有LOS则无需绕行
            if (GenSight.LineOfSight(shooterPos, targetPos, map, skipFirstCell: true))
                return;

            cachedRoute = ObstacleRouter.ComputeRoute(shooterPos, targetPos, map);
            if (cachedRoute == null) return;

            // ★ 逐段LOS验证：不通的一侧置null，两侧都不通则整条路由作废。
            // 原因：ObstacleRouter基于轮廓几何选锚点，不保证每段都有视线。
            //       若一侧不通而仍交替分配，该侧子弹会撞墙。
            var r = cachedRoute.Value;
            if (!IsPathClear(shooterPos, r.LeftAnchors, targetPos, map))
                r.LeftAnchors = null;
            if (!IsPathClear(shooterPos, r.RightAnchors, targetPos, map))
                r.RightAnchors = null;
            cachedRoute = r.IsValid ? (ObstacleRouteResult?)r : null;
        }

        /// <summary>
        /// 每颗子弹调用，交替分配左/右绕行路径。
        /// cachedRoute为null或无效时静默跳过（子弹直射）。
        /// </summary>
        public void AttachAutoRouteFlight(Projectile proj,
            LocalTargetInfo finalTarget, float anchorSpread)
        {
            if (cachedRoute == null || !cachedRoute.Value.IsValid) return;
            if (!(proj is Bullet_BDP bdp)) return;

            var route = cachedRoute.Value;
            List<IntVec3> anchors;
            // 两侧都可绕行时交替分配
            if (route.LeftAnchors != null && route.RightAnchors != null)
                anchors = (autoRouteIndex % 2 == 0)
                    ? route.LeftAnchors : route.RightAnchors;
            else
                anchors = route.LeftAnchors ?? route.RightAnchors;
            autoRouteIndex++;

            // v5解耦：通过宿主API注入引导路径
            bdp.TryInitGuidedFlight(anchors, finalTarget, anchorSpread);
        }

        /// <summary>
        /// 逐段LOS检查：射手→锚点1→…→锚点N→目标，任一段不通即返回false。
        /// anchors为null或空时视为不可用。
        /// internal：AnchorTargetingHelper预览绘制也需要调用。
        /// </summary>
        internal static bool IsPathClear(
            IntVec3 shooterPos, List<IntVec3> anchors, IntVec3 targetPos, Map map)
        {
            if (anchors == null || anchors.Count == 0) return false;

            // 射手→首锚点
            if (!GenSight.LineOfSight(shooterPos, anchors[0], map)) return false;
            // 锚点间
            for (int i = 0; i < anchors.Count - 1; i++)
            {
                if (!GenSight.LineOfSight(anchors[i], anchors[i + 1], map)) return false;
            }
            // 末锚点→目标
            if (!GenSight.LineOfSight(anchors[anchors.Count - 1], targetPos, map)) return false;

            return true;
        }

        /// <summary>重置所有状态。</summary>
        public void Reset()
        {
            RawAnchors = null;
            RawFinalTarget = default;
            CachedAnchorSpread = 0f;
            ManualAnchorsActive = false;
            ManualTargetCell = default;
            SavedThingTarget = default;
            LeftHasPath = false;
            RightHasPath = false;
            CurrentShotHasPath = false;
            cachedRoute = null;
            autoRouteIndex = 0;
            autoRouteCastPrepared = false;
            autoRouteFinalTarget = default;
            autoRouteLosCell = default;
        }
    }
}
