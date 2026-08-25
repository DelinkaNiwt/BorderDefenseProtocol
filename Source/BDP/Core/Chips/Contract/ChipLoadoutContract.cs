using System.Collections.Generic;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片装载声明结果。
    /// 它只回答 Trigger 会用到的装载语义。
    /// </summary>
    internal sealed class ChipLoadoutContract
    {
        /// <summary>
        /// 当前芯片所属的槽位区域。
        /// </summary>
        public ChipSlotRegion SlotRegion;

        /// <summary>
        /// 当前芯片对物理槽位的占用方式。
        /// </summary>
        public ChipSlotOccupancy SlotOccupancy;

        /// <summary>
        /// 当前芯片引用的启用互斥组集合。
        /// </summary>
        public IReadOnlyList<ChipExclusionGroupDef> ActivationExclusionGroups;

        /// <summary>
        /// 当前芯片的激活阶段音效声明结果。
        /// </summary>
        public ChipActivationAudioContract ActivationAudio;

        /// <summary>
        /// 当前芯片声明的启用延迟游戏刻。
        /// 小于 0 表示作者未填写，运行时应回退默认值。
        /// </summary>
        public int ActivationDelayTicks;

        /// <summary>
        /// 当前芯片声明的停用延迟游戏刻。
        /// 小于 0 表示作者未填写，运行时应回退默认值。
        /// </summary>
        public int DeactivationDelayTicks;

    }
}
