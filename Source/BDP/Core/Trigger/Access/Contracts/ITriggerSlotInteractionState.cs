using System.Collections.Generic;
using BDP.Core.Requirements;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 单个槽位的正式交互语义结果。
    /// 它描述的是“外部现在应如何理解这个槽位”，而不是槽位内部真值本体。
    /// </summary>
    public interface ITriggerSlotInteractionState
    {
        /// <summary>
        /// 当前被解释的槽位侧别。
        /// </summary>
        TriggerSide Side { get; }

        /// <summary>
        /// 当前被解释的槽位索引。
        /// </summary>
        int SlotIndex { get; }

        /// <summary>
        /// 当前正式控制槽位所在侧别。
        /// 普通槽位时等于自身；镜像副本时指向绑定根槽位。
        /// </summary>
        TriggerSide ControlSide { get; }

        /// <summary>
        /// 当前正式控制槽位索引。
        /// 普通槽位时等于自身；镜像副本时指向绑定根槽位。
        /// </summary>
        int ControlSlotIndex { get; }

        /// <summary>
        /// 当前槽位是否应被当成独立直接控制入口。
        /// 镜像副本通常为 false。
        /// </summary>
        bool IsDirectControlSlot { get; }

        /// <summary>
        /// 当前槽位对外应被理解成的正式动作类型。
        /// </summary>
        TriggerInteractionOperationKind OperationKind { get; }

        /// <summary>
        /// 当前动作解释的可用性。
        /// </summary>
        TriggerInteractionAvailability Availability { get; }

        /// <summary>
        /// 当前动作解释成立或受阻的正式原因码。
        /// </summary>
        TriggerInteractionReason Reason { get; }

        /// <summary>
        /// 若外部发起该动作，预期会走的正式过渡模式。
        /// </summary>
        TriggerInteractionTransitionMode TransitionMode { get; }

        /// <summary>
        /// 当前角色对这枚芯片的全部有序激活条件快照。
        /// </summary>
        IReadOnlyList<PawnRequirementSnapshot> ActivationRequirements { get; }
    }
}
