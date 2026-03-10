namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击管线模块基接口
    /// 所有阶段模块的共同祖先
    /// </summary>
    public interface IShotModule
    {
        /// <summary>执行优先级（升序，值小先执行）</summary>
        int Priority { get; }
    }
}
