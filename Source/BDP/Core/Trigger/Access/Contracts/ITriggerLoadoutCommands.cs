using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 装载正式输入口。
    /// 它只承载会改变 Trigger 真值的正式请求，不承担任何读取职责。
    /// </summary>
    public interface ITriggerLoadoutCommands
    {
        /// <summary>
        /// 尝试向指定槽位装入芯片。
        /// </summary>
        bool TryLoadChip(TriggerSide side, int slotIndex, Thing chip);

        /// <summary>
        /// 尝试卸下指定槽位里的芯片。
        /// </summary>
        bool TryUnloadChip(TriggerSide side, int slotIndex);

        /// <summary>
        /// 请求激活指定槽位。
        /// 这里用“请求”而不是“直接激活”，是为了保留实现层做校验的空间。
        /// </summary>
        bool RequestActivate(TriggerSide side, int slotIndex);

        /// <summary>
        /// 请求关闭某一侧当前激活的槽位。
        /// </summary>
        bool RequestDeactivate(TriggerSide side);

        /// <summary>
        /// 销毁指定槽位中与目标 ThingID 匹配的已装载芯片。
        /// </summary>
        bool TryDestroyLoadedChip(TriggerSide side, int slotIndex, string expectedThingId);

        /// <summary>
        /// 请求把一枚正式启用的多形态芯片切换到指定形态。
        /// </summary>
        bool RequestSwitchChipMode(Thing chip, string targetModeKey);

        /// <summary>
        /// 请求把一枚正式启用的多形态芯片切换到作者顺序中的下一形态。
        /// </summary>
        bool RequestCycleChipMode(Thing chip);
    }
}
