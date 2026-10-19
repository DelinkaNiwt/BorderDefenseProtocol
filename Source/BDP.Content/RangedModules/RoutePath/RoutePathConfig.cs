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

        /// <summary>原版每格精度的能力定标参照，0.99对应健康20级常态。</summary>
        public float AimReferenceAccuracy = 0.99f;

        /// <summary>参照能力下的误差压缩强度；默认1保持其它路线使用者中性。</summary>
        public float AimReferenceErrorPower = 1f;

        /// <summary>误差压缩强度随实际射击能力成长的指数。</summary>
        public float AimErrorGrowthExponent = 1.58275869f;

        /// <summary>最后一个手动锚点到达后释放导航控制。</summary>
        public bool ReleaseControlAfterLastAnchor;

        /// <summary>零锚点允许后续模块决定直射或越顶，不调用自动寻路。</summary>
        public bool AllowUnroutedFinalTarget;

        /// <summary>是否允许把地面格作为最终目标。</summary>
        public bool AllowGroundFinal = true;

        /// <summary>是否允许把 Thing 作为最终目标。</summary>
        public bool AllowThingFinal = true;

        /// <summary>中间续段相对名义锚点的最大散布半径。</summary>
        public float IntermediateSpreadRadius = 0.625f;

        /// <summary>最终续段相对冻结最终落点的最大散布半径。</summary>
        public float FinalSpreadRadius = 0.30f;

        /// <summary>原版精度达到最高时仍保留的散布比例。</summary>
        public float HighAccuracySpreadScale = 0.25f;

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

        /// <summary>复制全部路线参数。</summary>
        public RoutePathConfig CloneTyped()
        {
            return new RoutePathConfig
            {
                MaxAnchors = MaxAnchors,
                AimReferenceAccuracy = AimReferenceAccuracy,
                AimReferenceErrorPower = AimReferenceErrorPower,
                AimErrorGrowthExponent = AimErrorGrowthExponent,
                ReleaseControlAfterLastAnchor = ReleaseControlAfterLastAnchor,
                AllowUnroutedFinalTarget = AllowUnroutedFinalTarget,
                AllowGroundFinal = AllowGroundFinal,
                AllowThingFinal = AllowThingFinal,
                IntermediateSpreadRadius = IntermediateSpreadRadius,
                FinalSpreadRadius = FinalSpreadRadius,
                HighAccuracySpreadScale = HighAccuracySpreadScale,
                EnableAutoRoute = EnableAutoRoute,
                AutoRouteMaxDepth = AutoRouteMaxDepth,
                AutoRouteAnchorsPerWall = AutoRouteAnchorsPerWall,
                AutoRouteMaxObstacleCells = AutoRouteMaxObstacleCells
            };
        }
    }
}
