using BDP.Core.AttackExecution;

namespace BDP.Content.RangedModules.RoutePath
{
    /// <summary>
    /// 路线引导模块配置。
    /// </summary>
    public sealed class RoutePathConfig : RangedModuleConfigNode
    {
        /// <summary>允许玩家追加的最大锚点数量。</summary>
        public int MaxAnchors = 8;

        /// <summary>是否允许把地面格作为最终目标。</summary>
        public bool AllowGroundFinal = true;

        /// <summary>是否允许把 Thing 作为最终目标。</summary>
        public bool AllowThingFinal = true;

        /// <summary>到达阶段用于匹配段末的容差半径。</summary>
        public float ArrivalTolerance = 0.35f;

        /// <summary>中间续段相对名义锚点的最大散布半径。</summary>
        public float IntermediateSpreadRadius = 0.625f;

        /// <summary>最终续段相对冻结最终落点的最大散布半径。</summary>
        public float FinalSpreadRadius = 0.30f;

        /// <summary>原版精度达到最高时仍保留的散布比例。</summary>
        public float HighAccuracySpreadScale = 0.25f;

        /// <summary>候选散布不安全时允许折半收缩的次数。</summary>
        public int SpreadSafetyShrinkSteps = 4;

        /// <summary>是否允许零手动锚点时尝试自动绕障路径。</summary>
        public bool EnableAutoRoute = true;

        /// <summary>自动路径递归尝试的最大深度。</summary>
        public int AutoRouteMaxDepth = 3;

        /// <summary>每个障碍团一侧最多选择的自动绕行锚点数量。</summary>
        public int AutoRouteAnchorsPerWall = 3;

        /// <summary>自动路径扩展单个障碍团时允许扫描的最大格子数。</summary>
        public int AutoRouteMaxObstacleCells = 200;

        /// <summary>复制当前路线引导配置。</summary>
        public override RangedModuleConfigNode Clone()
        {
            return CloneTyped();
        }

        public RoutePathConfig CloneTyped()
        {
            return new RoutePathConfig
            {
                MaxAnchors = MaxAnchors,
                AllowGroundFinal = AllowGroundFinal,
                AllowThingFinal = AllowThingFinal,
                ArrivalTolerance = ArrivalTolerance,
                IntermediateSpreadRadius = IntermediateSpreadRadius,
                FinalSpreadRadius = FinalSpreadRadius,
                HighAccuracySpreadScale = HighAccuracySpreadScale,
                SpreadSafetyShrinkSteps = SpreadSafetyShrinkSteps,
                EnableAutoRoute = EnableAutoRoute,
                AutoRouteMaxDepth = AutoRouteMaxDepth,
                AutoRouteAnchorsPerWall = AutoRouteAnchorsPerWall,
                AutoRouteMaxObstacleCells = AutoRouteMaxObstacleCells
            };
        }
    }
}
