namespace BDP.Core.Trigger
{
    /// <summary>
    /// 单侧整体的正式交互语义结果。
    /// 它用于回答“这一侧当前整体该怎么理解”，而不是替代槽位级解释。
    /// </summary>
    public interface ITriggerSideInteractionState
    {
        /// <summary>
        /// 当前被解释的侧别。
        /// </summary>
        TriggerSide Side { get; }

        /// <summary>
        /// 当前正式控制侧别。
        /// 若该侧只是镜像受控，这里会指向真正控制侧。
        /// </summary>
        TriggerSide ControlSide { get; }

        /// <summary>
        /// 当前正式控制槽位索引。
        /// 没有正式控制槽位时为 -1。
        /// </summary>
        int ControlSlotIndex { get; }

        /// <summary>
        /// 当前侧是否存在正式激活槽位。
        /// </summary>
        bool HasActiveSlot { get; }

        /// <summary>
        /// 当前侧是否存在正在进行的切换过程。
        /// </summary>
        bool IsSwitching { get; }

        /// <summary>
        /// 当前切换目标槽位索引。
        /// 没有目标时为 -1。
        /// </summary>
        int TargetSlotIndex { get; }

        /// <summary>
        /// 当前整体动作语义类型。
        /// </summary>
        TriggerInteractionOperationKind OperationKind { get; }

        /// <summary>
        /// 当前整体动作语义的可用性。
        /// </summary>
        TriggerInteractionAvailability Availability { get; }

        /// <summary>
        /// 当前整体动作语义的正式原因码。
        /// </summary>
        TriggerInteractionReason Reason { get; }

        /// <summary>
        /// 若外部发起该整体动作，预期会走的正式过渡模式。
        /// </summary>
        TriggerInteractionTransitionMode TransitionMode { get; }
    }
}
