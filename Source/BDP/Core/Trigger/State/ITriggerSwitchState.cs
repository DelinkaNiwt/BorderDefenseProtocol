namespace BDP.Core.Trigger
{
    /// <summary>
    /// 单侧局部切换只读结果。
    /// 它是对内部切换上下文的正式只读投影，不暴露内部可变对象本体。
    /// </summary>
    public interface ITriggerSwitchState
    {
        /// <summary>
        /// 当前局部切换阶段。
        /// </summary>
        SwitchPhase Phase { get; }

        /// <summary>
        /// 当前阶段结束的绝对 tick。
        /// 没有有效阶段时为 0。
        /// </summary>
        int PhaseEndTick { get; }

        /// <summary>
        /// 当前正在进入的目标槽位索引。
        /// 没有目标时为 -1。
        /// </summary>
        int TargetSlotIndex { get; }

        /// <summary>
        /// 当前正在停用的槽位索引。
        /// 没有停用对象时为 -1。
        /// </summary>
        int DeactivatingSlotIndex { get; }

        /// <summary>
        /// 当前启用延迟时长。
        /// </summary>
        int ActivationDelayDuration { get; }

        /// <summary>
        /// 当前停用延迟时长。
        /// </summary>
        int DeactivationDelayDuration { get; }

        /// <summary>
        /// 当前是否存在有效局部切换过程。
        /// </summary>
        bool IsActive { get; }
    }
}
