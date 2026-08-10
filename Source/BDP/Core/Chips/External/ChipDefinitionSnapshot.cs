using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义对外只读快照。
    /// 它只暴露外部内容业务需要的稳定字段，不泄漏 Core 内部契约和校验对象。
    /// </summary>
    public sealed class ChipDefinitionSnapshot
    {
        /// <summary>
        /// 当前快照对应的芯片 ThingDef。
        /// </summary>
        public ThingDef ThingDef { get; internal set; }

        /// <summary>
        /// 当前芯片是否通过正式定义校验。
        /// </summary>
        public bool IsValid { get; internal set; }

        /// <summary>
        /// 当前芯片所属的槽位区域。
        /// </summary>
        public ChipSlotRegion SlotRegion { get; internal set; }

        /// <summary>
        /// 当前芯片对物理槽位的占用方式。
        /// </summary>
        public ChipSlotOccupancy SlotOccupancy { get; internal set; }

        /// <summary>
        /// 当前芯片的 Trion 容量费用。
        /// </summary>
        public float CapacityCost { get; internal set; }

        /// <summary>
        /// 当前芯片的激活费用。
        /// </summary>
        public float ActivationCost { get; internal set; }

        /// <summary>
        /// 芯片启用前的等待时长，单位为游戏刻。
        /// </summary>
        public int ActivationDelayTicks { get; internal set; }

        /// <summary>
        /// 芯片停用前的等待时长，单位为游戏刻。
        /// </summary>
        public int DeactivationDelayTicks { get; internal set; }
    }
}
