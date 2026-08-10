using System.Collections.Generic;

namespace BDP.Core.Trigger.Projection
{
    /// <summary>
    /// Trigger 正式投影构建输入。
    /// 它把 owner 内部已经成立的槽位真值、切换真值和容器一致性压成一份只读输入，
    /// 供正式发布链消费，而不是再经由公共 reader 反向读取 owner 自己。
    /// </summary>
    internal sealed class TriggerProjectionBuildInput
    {
        /// <summary>
        /// 主侧槽位真值快照。
        /// </summary>
        public IReadOnlyList<TriggerSlotState> MainSlots { get; set; }

        /// <summary>
        /// 副侧槽位真值快照。
        /// </summary>
        public IReadOnlyList<TriggerSlotState> SubSlots { get; set; }

        /// <summary>
        /// 特殊侧槽位真值快照。
        /// </summary>
        public IReadOnlyList<TriggerSlotState> SpecialSlots { get; set; }

        /// <summary>
        /// 主侧切换上下文快照。
        /// </summary>
        public SwitchContext MainSwitchContext { get; set; }

        /// <summary>
        /// 副侧切换上下文快照。
        /// </summary>
        public SwitchContext SubSwitchContext { get; set; }

        /// <summary>
        /// 特殊侧切换上下文快照。
        /// </summary>
        public SwitchContext SpecialSwitchContext { get; set; }

        /// <summary>
        /// 主侧槽位引用与正式容器是否一致。
        /// </summary>
        public bool IsMainContainerConsistent { get; set; }

        /// <summary>
        /// 副侧槽位引用与正式容器是否一致。
        /// </summary>
        public bool IsSubContainerConsistent { get; set; }

        /// <summary>
        /// 特殊侧槽位引用与正式容器是否一致。
        /// </summary>
        public bool IsSpecialContainerConsistent { get; set; }
    }
}
