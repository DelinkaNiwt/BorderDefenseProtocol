namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 瞄准阶段模块——参与Resolve子步骤（单Tick，TryCastShot内）
    /// 产出AimIntent，由宿主合并为AimResult
    /// </summary>
    public interface IShotAimModule : IShotModule
    {
        AimIntent ResolveAim(ShotSession session);
    }
}
