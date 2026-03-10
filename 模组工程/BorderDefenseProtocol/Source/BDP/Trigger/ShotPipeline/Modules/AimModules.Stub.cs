namespace BDP.Trigger.ShotPipeline.Modules
{
    /// <summary>
    /// LOS 检查模块（Stub 实现）
    /// 完整实现将在 Chunk 2 中完成
    /// </summary>
    public class LosCheckModule : IShotAimModule
    {
        public int Priority => 10;

        public AimIntent ResolveAim(ShotSession session)
        {
            // Stub: 总是返回默认意图（不中止）
            return AimIntent.Default;
        }
    }

    /// <summary>
    /// 锚点瞄准模块（Stub 实现）
    /// 完整实现将在 Chunk 2 中完成
    /// </summary>
    public class AnchorAimModule : IShotAimModule
    {
        public int Priority => 20;

        public AimIntent ResolveAim(ShotSession session)
        {
            // Stub: 不产出锚点
            return AimIntent.Default;
        }
    }

    /// <summary>
    /// 自动绕行瞄准模块（Stub 实现）
    /// 完整实现将在 Chunk 2 中完成
    /// </summary>
    public class AutoRouteAimModule : IShotAimModule
    {
        public int Priority => 30;

        public AimIntent ResolveAim(ShotSession session)
        {
            // Stub: 不产出路由
            return AimIntent.Default;
        }
    }
}
