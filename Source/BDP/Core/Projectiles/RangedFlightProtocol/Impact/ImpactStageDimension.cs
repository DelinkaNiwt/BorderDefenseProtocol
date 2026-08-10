namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// Impact 阶段允许发生覆盖裁决的正式维度。
    /// 直伤和范围方案分别独立裁决。
    /// </summary>
    internal enum ImpactStageDimension
    {
        DirectDamage = 0,
        AreaEffect = 1
    }
}
