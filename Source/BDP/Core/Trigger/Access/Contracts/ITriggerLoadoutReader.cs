using System.Collections.Generic;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 装载正式读取口。
    /// 它只暴露已经成立的 Trigger 真值与最小正式读取结果，不承载任何写入动作。
    /// </summary>
    public interface ITriggerLoadoutReader
    {
        /// <summary>
        /// 读取当前触发体的芯片配置控制模式。
        /// </summary>
        TriggerLoadoutControlMode LoadoutControlMode { get; }

        /// <summary>
        /// 读取全部槽位。
        /// 固定顺序由实现自己保证，方便上层稳定遍历。
        /// </summary>
        IEnumerable<ITriggerSlotState> GetAllSlots();

        /// <summary>
        /// 按侧读取槽位。
        /// 这是 Trigger 对外的正式分组读取面。
        /// </summary>
        IEnumerable<ITriggerSlotState> GetSlots(TriggerSide side);

        /// <summary>
        /// 读取当前正式激活的槽位。
        /// 这里只看已经成立的逻辑状态，不把表现中过渡态混进来。
        /// </summary>
        IEnumerable<ITriggerSlotState> GetActiveSlots();

        /// <summary>
        /// 读取某一侧当前正式激活的槽位。
        /// </summary>
        ITriggerSlotState GetActiveSlot(TriggerSide side);

        /// <summary>
        /// 读取某一侧当前处于切换表现中的目标槽位。
        /// 它表达的是“看上去正在切换到谁”，不等于逻辑上已经激活。
        /// </summary>
        ITriggerSlotState GetActivatingSlot(TriggerSide side);

        /// <summary>
        /// 读取某一侧当前局部切换状态。
        /// 返回的是正式只读快照，不是内部上下文本体。
        /// </summary>
        ITriggerSwitchState GetSwitchState(TriggerSide side);

        /// <summary>
        /// 读取某枚芯片当前正式形态键。
        /// 当前若主模组尚未建立正式形态系统，则返回 null。
        /// </summary>
        string GetChipModeKey(Thing chip);

        /// <summary>
        /// 读取某枚芯片在当前形态内部的正式姿态键。
        /// 当前形态没有姿态或姿态尚未成立时返回 null。
        /// </summary>
        string GetChipStanceKey(Thing chip);

        /// <summary>
        /// 读取某枚芯片当前形态内的有序姿态选项。
        /// 当前形态没有多个姿态、定义无效或找不到根槽时返回空列表。
        /// </summary>
        IReadOnlyList<ChipStanceOptionSnapshot> GetChipStanceOptions(Thing chip);

        /// <summary>
        /// 读取某枚正式启用多形态芯片的有序形态选项。
        /// 未启用、单形态、定义无效或找不到根槽时返回空列表。
        /// </summary>
        IReadOnlyList<ChipModeOptionSnapshot> GetChipModeOptions(Thing chip);
    }
}
