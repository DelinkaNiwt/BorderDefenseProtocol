namespace BDP.Core.Projectiles.RangedFlightProtocol.Flight
{
    /// <summary>
    /// 飞行阶段的有限维度。
    /// 所有飞行模块都必须在这些有限维度内声明自己的影响点。
    /// </summary>
    public enum FlightDimension
    {
        Destination = 0,
        CurrentTarget = 1,
        ContinueFlight = 2
    }
}
