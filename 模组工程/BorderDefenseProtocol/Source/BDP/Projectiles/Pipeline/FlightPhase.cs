namespace BDP.Projectiles.Pipeline
{
    /// <summary>
    /// 飞行阶段枚举——宿主自动推导的状态描述（仅供vanilla适配和诊断）。
    /// 模块不再读写Phase，改为读写三层目标（AimTarget/LockedTarget/CurrentTarget）。
    /// </summary>
    public enum FlightPhase
    {
        /// <summary>直飞——三者恒等，无模块介入。</summary>
        Direct,
        /// <summary>引导中——CurrentTarget≠LockedTarget，有中间路径。</summary>
        Guided,
        /// <summary>追踪中——追踪模块活跃（有Intent产出且TrackingActivated）。</summary>
        Tracking,
        /// <summary>自由飞行——无模块介入，惯性飞行。</summary>
        Free
    }
}
