using System;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 层的最小事件口。
    /// 这里只广播状态变化，不持有任何业务结果。
    /// </summary>
    public interface ITriggerEvents
    {
        /// <summary>
        /// 槽位装载内容发生变化时触发。
        /// 包括装入和卸下。
        /// </summary>
        event Action<TriggerSlotStateChangedArgs> SlotLoadoutChanged;

        /// <summary>
        /// 某个槽位已正式成为当前激活槽时触发。
        /// 注意这不是“开始切换表现”，而是逻辑结果已经成立。
        /// </summary>
        event Action<TriggerSlotStateChangedArgs> SlotActivationCommitted;

        /// <summary>
        /// 当前激活槽被正式关闭时触发。
        /// </summary>
        event Action<TriggerSlotStateChangedArgs> SlotDeactivated;

        /// <summary>
        /// 某个槽位的禁用状态发生变化时触发。
        /// 它只表达“禁用真值变了”，不表达谁是上游原因宿主。
        /// </summary>
        event Action<TriggerSlotStateChangedArgs> SlotDisableStateChanged;
    }
}
