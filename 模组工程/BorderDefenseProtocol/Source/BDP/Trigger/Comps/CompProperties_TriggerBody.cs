using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 触发体的CompProperties——XML可配置参数。
    /// v2.0变更（T23）：mainSlotCount/subSlotCount/hasSub → leftSlotCount/rightSlotCount/hasRight
    /// </summary>
    public class CompProperties_TriggerBody : CompProperties
    {
        /// <summary>左侧槽位数量（默认4）。</summary>
        public int leftSlotCount = 4;

        /// <summary>右侧槽位数量（默认4）。</summary>
        public int rightSlotCount = 4;

        /// <summary>是否有右侧（某些近界触发器可能只有单侧）。</summary>
        public bool hasRight = true;

        /// <summary>切换空窗期长度（ticks，默认30≈0.5秒）。</summary>
        public int switchCooldownTicks = 30;

        /// <summary>玩家是否可以装载/卸载芯片（BORDER=true，近界/黑=false）。</summary>
        public bool allowChipManagement = true;

        /// <summary>装备时是否自动激活预装芯片（近界/黑触发器用）。</summary>
        public bool autoActivateOnEquip = false;

        /// <summary>首次生成时自动装载的芯片列表（近界/黑触发器用）。</summary>
        public List<PreloadedChipConfig> preloadedChips;

        public CompProperties_TriggerBody()
        {
            compClass = typeof(CompTriggerBody);
        }
    }

    /// <summary>预装芯片配置（近界/黑触发器用）。</summary>
    public class PreloadedChipConfig
    {
        public SlotSide side = SlotSide.Left;
        public int slotIndex = 0;
        public ThingDef chipDef;
    }
}
