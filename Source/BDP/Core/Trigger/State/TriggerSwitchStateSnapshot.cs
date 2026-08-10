namespace BDP.Core.Trigger
{
    /// <summary>
    /// 单侧局部切换正式只读快照。
    /// 它把内部运行时上下文压成对外可稳定读取的结果对象。
    /// </summary>
    public sealed class TriggerSwitchStateSnapshot : ITriggerSwitchState
    {
        /// <summary>
        /// 当前局部切换阶段。
        /// </summary>
        public SwitchPhase Phase { get; private set; }

        /// <summary>
        /// 当前阶段结束的绝对 tick。
        /// </summary>
        public int PhaseEndTick { get; private set; }

        /// <summary>
        /// 当前正在进入的目标槽位索引。
        /// </summary>
        public int TargetSlotIndex { get; private set; }

        /// <summary>
        /// 当前正在停用的槽位索引。
        /// </summary>
        public int DeactivatingSlotIndex { get; private set; }

        /// <summary>
        /// 当前启用延迟时长。
        /// </summary>
        public int ActivationDelayDuration { get; private set; }

        /// <summary>
        /// 当前停用延迟时长。
        /// </summary>
        public int DeactivationDelayDuration { get; private set; }

        /// <summary>
        /// 当前是否存在有效局部切换过程。
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 构造一个空闲态切换快照。
        /// 用它避免外部读取者再去猜 null 的含义。
        /// </summary>
        public static TriggerSwitchStateSnapshot Idle()
        {
            return new TriggerSwitchStateSnapshot
            {
                Phase = SwitchPhase.Idle,
                PhaseEndTick = 0,
                TargetSlotIndex = -1,
                DeactivatingSlotIndex = -1,
                ActivationDelayDuration = 0,
                DeactivationDelayDuration = 0,
                IsActive = false
            };
        }

        /// <summary>
        /// 基于当前内部切换上下文构造正式只读快照。
        /// </summary>
        public static TriggerSwitchStateSnapshot FromContext(SwitchContext context)
        {
            if (context == null)
            {
                return Idle();
            }

            return new TriggerSwitchStateSnapshot
            {
                Phase = context.phase,
                PhaseEndTick = context.phaseEndTick,
                TargetSlotIndex = context.targetSlotIndex,
                DeactivatingSlotIndex = context.deactivatingSlotIndex,
                ActivationDelayDuration = context.activationDelayDuration,
                DeactivationDelayDuration = context.deactivationDelayDuration,
                IsActive = true
            };
        }
    }
}
