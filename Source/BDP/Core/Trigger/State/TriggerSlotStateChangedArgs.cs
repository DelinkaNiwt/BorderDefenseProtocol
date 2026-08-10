using System;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 槽位状态变化事件参数。
    /// 用来在不暴露宿主内部实现的前提下广播局部变化。
    /// </summary>
    public sealed class TriggerSlotStateChangedArgs : EventArgs
    {
        /// <summary>
        /// 哪一侧发生了变化。
        /// </summary>
        public TriggerSide Side;

        /// <summary>
        /// 该侧中的哪个槽位发生了变化。
        /// </summary>
        public int SlotIndex;

        /// <summary>
        /// 本次变化关联的芯片。
        /// 卸载时这里保留的是被卸下的旧芯片，方便外层识别变化对象。
        /// </summary>
        public Thing Chip;

        /// <summary>
        /// 本次变化后的禁用原因码。
        /// 只在禁用状态变化事件中保证有明确语义。
        /// </summary>
        public TriggerDisableReason DisabledReason;
    }
}
