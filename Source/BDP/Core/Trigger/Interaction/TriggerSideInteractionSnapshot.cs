namespace BDP.Core.Trigger
{
    /// <summary>
    /// 单侧整体的正式交互语义快照。
    /// 它用于外部调用者稳定读取某一侧当前整体该如何理解。
    /// </summary>
    public sealed class TriggerSideInteractionSnapshot : ITriggerSideInteractionState
    {
        /// <summary>
        /// 当前被解释的侧别。
        /// </summary>
        public TriggerSide Side { get; private set; }

        /// <summary>
        /// 当前正式控制侧别。
        /// </summary>
        public TriggerSide ControlSide { get; private set; }

        /// <summary>
        /// 当前正式控制槽位索引。
        /// </summary>
        public int ControlSlotIndex { get; private set; }

        /// <summary>
        /// 当前侧是否存在正式激活槽位。
        /// </summary>
        public bool HasActiveSlot { get; private set; }

        /// <summary>
        /// 当前侧是否存在进行中的切换。
        /// </summary>
        public bool IsSwitching { get; private set; }

        /// <summary>
        /// 当前切换目标槽位索引。
        /// </summary>
        public int TargetSlotIndex { get; private set; }

        /// <summary>
        /// 当前整体动作语义类型。
        /// </summary>
        public TriggerInteractionOperationKind OperationKind { get; private set; }

        /// <summary>
        /// 当前整体动作语义的可用性。
        /// </summary>
        public TriggerInteractionAvailability Availability { get; private set; }

        /// <summary>
        /// 当前整体动作语义的原因码。
        /// </summary>
        public TriggerInteractionReason Reason { get; private set; }

        /// <summary>
        /// 当前整体动作语义的正式过渡模式。
        /// </summary>
        public TriggerInteractionTransitionMode TransitionMode { get; private set; }

        /// <summary>
        /// 构造一个完整的侧级交互语义快照。
        /// </summary>
        public static TriggerSideInteractionSnapshot Create(
            TriggerSide side,
            TriggerSide controlSide,
            int controlSlotIndex,
            bool hasActiveSlot,
            bool isSwitching,
            int targetSlotIndex,
            TriggerInteractionOperationKind operationKind,
            TriggerInteractionAvailability availability,
            TriggerInteractionReason reason,
            TriggerInteractionTransitionMode transitionMode)
        {
            return new TriggerSideInteractionSnapshot
            {
                Side = side,
                ControlSide = controlSide,
                ControlSlotIndex = controlSlotIndex,
                HasActiveSlot = hasActiveSlot,
                IsSwitching = isSwitching,
                TargetSlotIndex = targetSlotIndex,
                OperationKind = operationKind,
                Availability = availability,
                Reason = reason,
                TransitionMode = transitionMode
            };
        }
    }
}
