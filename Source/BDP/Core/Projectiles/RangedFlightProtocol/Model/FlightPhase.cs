namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 投射物正式飞行阶段。
    /// 它是前后半段共享的有限阶段枚举，不是开放式玩法名。
    /// </summary>
    public enum FlightPhase
    {
        None = 0,
        Initial = 1,
        Routed = 2,
        Adaptive = 3,
        Curved = 4,
        Terminal = 5
    }
}
