namespace BDP.Core.Projectiles.RangedFlightProtocol.Projection
{
    /// <summary>
    /// 后半段投影层边界说明。
    /// Trail 和外部信息读取只允许消费 Flight/Hit/Impact 摘要。
    /// </summary>
    internal static class RangedFlightProjectionBoundary
    {
        public const string TrailInput = "FlightRecord + RangedProjectionFeed";
        public const string ImpactVisualInput = "HitRecord + ImpactPlan + RangedProjectionFeed";
        public const string ExternalInfoInput = "只读阶段摘要，不触碰模块内部状态";
    }
}
