namespace BDP.Trigger
{
    /// <summary>
    /// 飞行阶段枚举——模块间唯一协作媒介。
    /// 模块只读Phase判断自身行为，宿主统一管理Phase转换。
    /// </summary>
    public enum FlightPhase
    {
        /// <summary>直飞（默认）——无引导无追踪，vanilla弹道。</summary>
        Direct,
        /// <summary>引导段——GuidedModule折线飞行中。</summary>
        GuidedLeg,
        /// <summary>最终进近——引导结束或追踪极近距离，飞向最终目标。</summary>
        FinalApproach,
        /// <summary>追踪中——TrackingModule已激活并锁定目标。</summary>
        Tracking,
        /// <summary>追踪丢失——曾锁定但当前无有效目标。</summary>
        TrackingLost,
        /// <summary>自由飞行——追踪彻底失败，沿惯性方向飞行直到超时。</summary>
        Free
    }
}
