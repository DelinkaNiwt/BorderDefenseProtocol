namespace BDP.Core.AttackExecution.RangedProtocol.ProjectileInit
{
    /// <summary>
    /// ProjectileInit 阶段允许发生覆盖裁决的正式维度。
    /// 路径种子相关字段视为同一条初始化骨架。
    /// </summary>
    internal enum ProjectileInitStageDimension
    {
        Origin = 0,
        AimTarget = 1,
        CurrentTarget = 2,
        InitialBallistics = 3
    }
}
