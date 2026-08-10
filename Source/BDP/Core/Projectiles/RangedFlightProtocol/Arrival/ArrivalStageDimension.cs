namespace BDP.Core.Projectiles.RangedFlightProtocol.Arrival
{
    /// <summary>
    /// Arrival 阶段允许发生覆盖裁决的正式维度。
    /// 到达后是否继续飞、飞向哪里、追谁，都在这里定义。
    /// </summary>
    internal enum ArrivalStageDimension
    {
        ContinueFlight = 0,
        NextDestination = 1
    }
}
