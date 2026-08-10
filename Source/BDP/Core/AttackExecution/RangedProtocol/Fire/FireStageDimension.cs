namespace BDP.Core.AttackExecution.RangedProtocol.Fire
{
    /// <summary>
    /// Fire 阶段允许发生覆盖裁决的正式维度。
    /// emit 级覆盖只在对应 emit 内生效。
    /// </summary>
    internal enum FireStageDimension
    {
        Projectile = 0,
        FireCount = 1,
        Abort = 2,
        EmitProjectile = 3
    }
}
