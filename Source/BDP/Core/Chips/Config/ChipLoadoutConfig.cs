using System.Collections.Generic;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片装载配置。
    /// 它只描述装载语义，不描述最终表达结果。
    /// </summary>
    public sealed class ChipLoadoutConfig : IExposable
    {
        /// <summary>
        /// 当前芯片所属的槽位区域。
        /// 必须由作者显式填写；未填写时保留为 Unspecified 并由定义校验拒绝。
        /// </summary>
        public ChipSlotRegion SlotRegion = ChipSlotRegion.Unspecified;

        /// <summary>
        /// 当前芯片对物理槽位的占用方式。
        /// 必须由作者显式填写；未填写时保留为 Unspecified 并由定义校验拒绝。
        /// </summary>
        public ChipSlotOccupancy SlotOccupancy = ChipSlotOccupancy.Unspecified;

        /// <summary>
        /// 当前芯片声明的启用互斥组。
        /// 与其它芯片共享任意一个组时，两者不能同时启用。
        /// </summary>
        public List<ChipExclusionGroupDef> ActivationExclusionGroups;

        /// <summary>
        /// 当前芯片可选的激活阶段音效声明。
        /// </summary>
        public ChipActivationAudioConfig ActivationAudio;

        /// <summary>
        /// 当前芯片从收到启用命令到正式生效的延迟游戏刻。
        /// -1 表示作者未填写，此时运行时回退到系统默认值。
        /// </summary>
        public int ActivationDelayTicks = -1;

        /// <summary>
        /// 当前芯片从收到停用命令到正式失效的延迟游戏刻。
        /// -1 表示作者未填写，此时运行时回退到系统默认值。
        /// </summary>
        public int DeactivationDelayTicks = -1;

        /// <summary>
        /// RimWorld XML 反序列化兼容口。
        /// 当前保持最小空实现即可。
        /// </summary>
        public void ExposeData()
        {
        }
    }
}
