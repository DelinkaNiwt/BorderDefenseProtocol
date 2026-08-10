namespace BDP.Core.AttackExecution.RangedProtocol.Aim
{
    /// <summary>
    /// Aim 阶段允许发生覆盖裁决的正式维度。
    /// 后装配模块只会覆盖自己实际触及的瞄准维度。
    /// </summary>
    internal enum AimStageDimension
    {
        Target = 0,
        Accuracy = 1,
        ForcedMissRadius = 2,
        Abort = 3
    }
}
