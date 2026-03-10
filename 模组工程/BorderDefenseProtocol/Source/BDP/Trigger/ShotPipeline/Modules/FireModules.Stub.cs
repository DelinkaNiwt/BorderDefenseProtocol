namespace BDP.Trigger.ShotPipeline.Modules
{
    /// <summary>
    /// 齐射扩散模块（Stub 实现）
    /// 完整实现将在 Chunk 3 中完成
    /// </summary>
    public class VolleySpreadModule : IShotFireModule
    {
        public int Priority => 10;

        public FireIntent OnFire(ShotSession session)
        {
            // Stub: 返回默认意图（不修改任何值）
            return FireIntent.Default;
        }
    }

    /// <summary>
    /// Trion 消耗模块（Stub 实现）
    /// 完整实现将在 Chunk 3 中完成
    /// </summary>
    public class TrionCostModule : IShotFireModule
    {
        public int Priority => 20;

        public FireIntent OnFire(ShotSession session)
        {
            // Stub: 不消耗 Trion
            return FireIntent.Default;
        }
    }

    /// <summary>
    /// 飞行数据模块（Stub 实现）
    /// 完整实现将在 Chunk 3 中完成
    /// </summary>
    public class FlightDataModule : IShotFireModule
    {
        public int Priority => 30;

        public FireIntent OnFire(ShotSession session)
        {
            // Stub: 不产出飞行数据
            return FireIntent.Default;
        }
    }

    /// <summary>
    /// 自动绕行射击模块（Stub 实现）
    /// 完整实现将在 Chunk 3 中完成
    /// </summary>
    public class AutoRouteFireModule : IShotFireModule
    {
        public int Priority => 40;

        public FireIntent OnFire(ShotSession session)
        {
            // Stub: 不修改飞行数据
            return FireIntent.Default;
        }
    }
}
