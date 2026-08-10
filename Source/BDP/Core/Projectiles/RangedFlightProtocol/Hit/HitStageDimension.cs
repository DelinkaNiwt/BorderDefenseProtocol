namespace BDP.Core.Projectiles.RangedFlightProtocol.Hit
{
    /// <summary>
    /// Hit 阶段允许发生覆盖裁决的正式维度。
    /// 命中对象、命中点与命中后持续状态都按这些维度裁决。
    /// </summary>
    internal enum HitStageDimension
    {
        HitThing = 0,
        HitCell = 1,
        ForceGround = 2
    }
}
