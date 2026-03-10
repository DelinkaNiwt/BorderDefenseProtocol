using System.Collections.Generic;
using BDP.Projectiles;
using BDP.Projectiles.Config;
using UnityEngine;
using Verse;

namespace BDP.Trigger.ShotPipeline.Modules
{
    /// <summary>
    /// 自动绕行瞄准模块
    /// Priority必须高于LosCheckModule（数值更大）
    /// 当LOS失败但有绕行路径时，取消Abort并设置绕行锚点
    ///
    /// 迁移自 VerbFlightState.PrepareAutoRoute() 和 InterceptCastTarget()
    /// </summary>
    public class AutoRouteAimModule : IShotAimModule, IShotAimValidator
    {
        public int Priority { get; }

        /// <summary>
        /// 缓存的绕行路由结果（每次 ResolveAim 计算一次）
        /// </summary>
        private ObstacleRouteResult? cachedRoute;

        /// <summary>
        /// 齐射索引（用于交替分配左右路径）
        /// </summary>
        private int autoRouteIndex;

        /// <summary>
        /// 默认构造函数（优先级 30）
        /// </summary>
        public AutoRouteAimModule() : this(30) { }

        /// <summary>
        /// 带优先级的构造函数
        /// </summary>
        public AutoRouteAimModule(int priority)
        {
            Priority = priority;
        }

        /// <summary>
        /// 验证目标（目标选择阶段）
        /// 自动绕行始终允许目标选择——路由计算在 ResolveAim 阶段
        /// </summary>
        public AimValidation ValidateTarget(ShotSession session, LocalTargetInfo target)
        {
            // 不阻止目标选择，LosCheckModule 负责基础检查
            return AimValidation.Valid;
        }

        /// <summary>
        /// 解析瞄准意图（射击执行阶段）
        /// 迁移自 VerbFlightState.PrepareAutoRoute() 和 InterceptCastTarget()
        /// </summary>
        public AimIntent ResolveAim(ShotSession session)
        {
            var intent = AimIntent.Default;
            var ctx = session.Context;

            // 检查是否可以自动绕行
            if (!CanAutoRoute(ctx))
                return intent;

            // 如果有直接 LOS，不需要绕行
            if (GenSight.LineOfSight(ctx.CasterPosition, ctx.Target.Cell, ctx.Caster.Map))
                return intent;

            // 获取弹药的引导配置
            var projectileDef = ctx.ChipConfig?.GetPrimaryProjectileDef();
            if (projectileDef == null)
                return intent;

            var guidedCfg = projectileDef.GetModExtension<BDPGuidedConfig>();
            if (guidedCfg == null)
                return intent;

            // 计算绕行路由（迁移自 VerbFlightState.PrepareAutoRoute）
            cachedRoute = ComputeAutoRoute(
                ctx.CasterPosition,
                ctx.Target.Cell,
                ctx.Caster.Map,
                guidedCfg.maxRouteDepth,
                guidedCfg.anchorsPerWall
            );

            if (cachedRoute == null || !cachedRoute.Value.IsValid)
                return intent;

            // 将路由结果写入 ShotSession，供 AutoRouteFireModule 读取
            session.RouteResult = cachedRoute;

            // 选择首锚点作为 LOS 检查目标（迁移自 TryPickLosAnchor）
            if (!TryPickLosAnchor(cachedRoute.Value, ctx.CasterPosition, out IntVec3 losAnchor))
                return intent;

            // 取消 LosCheckModule 的 abort，设置绕行锚点
            intent.AbortShot = false;

            // 选择一侧路径作为锚点（交替分配）
            var route = cachedRoute.Value;
            List<IntVec3> anchors = SelectRouteSide(route);

            if (anchors != null && anchors.Count > 0)
            {
                intent.AnchorPoints = anchors.ToArray();
                // 使用 Verb 层的 anchorSpread 配置
                intent.AnchorSpread = ctx.GuidedConfig?.anchorSpread ?? 0.3f;
            }

            return intent;
        }

        // ══════════════════════════════════════════
        //  私有辅助方法
        // ══════════════════════════════════════════

        /// <summary>
        /// 检查是否可以自动绕行
        /// 条件：
        /// 1. 弹药有 BDPGuidedConfig
        /// 2. Verb 层配置了 guided
        /// 3. Verb 不支持手动引导（手动引导优先）
        /// </summary>
        private bool CanAutoRoute(ShotContext ctx)
        {
            // 检查 Verb 层是否配置了引导飞行
            if (ctx.RangedConfig?.guided == null)
                return false;

            // 手动引导优先，自动绕行作为 fallback
            if (ctx.Verb.SupportsGuided)
                return false;

            return true;
        }

        /// <summary>
        /// 计算自动绕行路由
        /// 迁移自 VerbFlightState.PrepareAutoRoute()
        /// </summary>
        private ObstacleRouteResult? ComputeAutoRoute(
            IntVec3 shooterPos,
            IntVec3 targetPos,
            Map map,
            int maxDepth,
            int anchorsPerWall)
        {
            // 分别构建左优先、右优先两条迭代绕行路径
            var leftAnchors = ObstacleRouter.ComputeIterativeRoute(
                shooterPos, targetPos, map, maxDepth, anchorsPerWall, preferLeft: true);
            var rightAnchors = ObstacleRouter.ComputeIterativeRoute(
                shooterPos, targetPos, map, maxDepth, anchorsPerWall, preferLeft: false);

            // 最终全路径 LOS 验证，任一段不通则该侧作废
            if (!ObstacleRouter.IsPathClear(shooterPos, leftAnchors, targetPos, map))
                leftAnchors = null;
            if (!ObstacleRouter.IsPathClear(shooterPos, rightAnchors, targetPos, map))
                rightAnchors = null;

            if (leftAnchors == null && rightAnchors == null)
                return null;

            return new ObstacleRouteResult
            {
                LeftAnchors = leftAnchors,
                RightAnchors = rightAnchors
            };
        }

        /// <summary>
        /// 为自动绕行施法选择一个首锚点（两侧都可用时选更近的一侧）
        /// 迁移自 VerbFlightState.TryPickLosAnchor()
        /// </summary>
        private static bool TryPickLosAnchor(
            ObstacleRouteResult route,
            IntVec3 shooterPos,
            out IntVec3 anchor)
        {
            anchor = default;
            bool hasLeft = route.LeftAnchors != null && route.LeftAnchors.Count > 0;
            bool hasRight = route.RightAnchors != null && route.RightAnchors.Count > 0;

            if (!hasLeft && !hasRight)
                return false;

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
        /// 选择一侧路径（交替分配左右）
        /// 迁移自 VerbFlightState.AttachAutoRouteFlight()
        /// </summary>
        private List<IntVec3> SelectRouteSide(ObstacleRouteResult route)
        {
            List<IntVec3> anchors;

            // 两侧都可绕行时交替分配
            if (route.LeftAnchors != null && route.RightAnchors != null)
            {
                anchors = (autoRouteIndex % 2 == 0)
                    ? route.LeftAnchors
                    : route.RightAnchors;
            }
            else
            {
                anchors = route.LeftAnchors ?? route.RightAnchors;
            }

            autoRouteIndex++;
            return anchors;
        }
    }
}
