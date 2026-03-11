namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击阶段模块——在TryCastShot内、AimResult确定后调用
    /// 产出FireIntent，由宿主合并为FireResult
    /// </summary>
    public interface IShotFireModule : IShotModule
    {
        FireIntent OnFire(ShotSession session);
    }
}
