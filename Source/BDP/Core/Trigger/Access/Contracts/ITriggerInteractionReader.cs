using System.Collections.Generic;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 正式交互语义读取口。
    /// 它不返回 UI 控件，只返回外部调用者此刻应如何理解槽位与侧别操作的正式结果。
    /// </summary>
    public interface ITriggerInteractionReader
    {
        /// <summary>
        /// 读取全部槽位的交互语义结果。
        /// 固定顺序由实现保证，方便外部稳定遍历。
        /// </summary>
        IEnumerable<ITriggerSlotInteractionState> GetAllSlotInteractions();

        /// <summary>
        /// 按侧读取槽位交互语义结果。
        /// </summary>
        IEnumerable<ITriggerSlotInteractionState> GetSlotInteractions(TriggerSide side);

        /// <summary>
        /// 读取某个指定槽位当前的交互语义结果。
        /// </summary>
        ITriggerSlotInteractionState GetSlotInteraction(TriggerSide side, int slotIndex);

        /// <summary>
        /// 读取某一侧当前的整体交互语义结果。
        /// 它用于解释该侧整体应被理解成关闭、切换、镜像受控还是当前没有正式动作。
        /// </summary>
        ITriggerSideInteractionState GetSideInteraction(TriggerSide side);
    }
}
