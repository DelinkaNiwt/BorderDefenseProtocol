using System.Collections.Generic;
using BDP.Core.Requirements;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 单个槽位的正式交互语义快照。
    /// 它把内部规则解释结果压成稳定只读对象，供外部调用者消费。
    /// </summary>
    public sealed class TriggerSlotInteractionSnapshot : ITriggerSlotInteractionState
    {
        /// <summary>
        /// 当前被解释的槽位侧别。
        /// </summary>
        public TriggerSide Side { get; private set; }

        /// <summary>
        /// 当前被解释的槽位索引。
        /// </summary>
        public int SlotIndex { get; private set; }

        /// <summary>
        /// 当前正式控制槽位侧别。
        /// </summary>
        public TriggerSide ControlSide { get; private set; }

        /// <summary>
        /// 当前正式控制槽位索引。
        /// </summary>
        public int ControlSlotIndex { get; private set; }

        /// <summary>
        /// 当前槽位是否是直接控制入口。
        /// </summary>
        public bool IsDirectControlSlot { get; private set; }

        /// <summary>
        /// 当前对外动作语义类型。
        /// </summary>
        public TriggerInteractionOperationKind OperationKind { get; private set; }

        /// <summary>
        /// 当前动作语义的可用性。
        /// </summary>
        public TriggerInteractionAvailability Availability { get; private set; }

        /// <summary>
        /// 当前动作语义的原因码。
        /// </summary>
        public TriggerInteractionReason Reason { get; private set; }

        /// <summary>
        /// 当前动作语义对应的正式过渡模式。
        /// </summary>
        public TriggerInteractionTransitionMode TransitionMode { get; private set; }

        /// <summary>
        /// 当前角色对这枚芯片的全部有序激活条件快照。
        /// </summary>
        public IReadOnlyList<PawnRequirementSnapshot> ActivationRequirements { get; private set; }

        /// <summary>
        /// 构造一个完整的槽位交互语义快照。
        /// </summary>
        public static TriggerSlotInteractionSnapshot Create(
            TriggerSide side,
            int slotIndex,
            TriggerSide controlSide,
            int controlSlotIndex,
            bool isDirectControlSlot,
            TriggerInteractionOperationKind operationKind,
            TriggerInteractionAvailability availability,
            TriggerInteractionReason reason,
            TriggerInteractionTransitionMode transitionMode,
            IReadOnlyList<PawnRequirementSnapshot> activationRequirements = null)
        {
            return new TriggerSlotInteractionSnapshot
            {
                Side = side,
                SlotIndex = slotIndex,
                ControlSide = controlSide,
                ControlSlotIndex = controlSlotIndex,
                IsDirectControlSlot = isDirectControlSlot,
                OperationKind = operationKind,
                Availability = availability,
                Reason = reason,
                TransitionMode = transitionMode,
                ActivationRequirements = activationRequirements
                    ?? new List<PawnRequirementSnapshot>().AsReadOnly()
            };
        }
    }
}
