using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 触发体的CompProperties——XML可配置参数。
    /// v2.0变更（T23）：mainSlotCount/subSlotCount/hasSub → leftSlotCount/rightSlotCount/hasRight
    /// v3.0变更：leftSlotCount/rightSlotCount/hasRight → leftHandSlotCount/rightHandSlotCount/hasRightHand
    /// </summary>
    public class CompProperties_TriggerBody : CompProperties
    {
        /// <summary>左手槽位数量（默认4）。</summary>
        public int leftHandSlotCount = 4;

        /// <summary>右手槽位数量（默认4）。</summary>
        public int rightHandSlotCount = 4;

        /// <summary>是否有右手槽（某些近界触发器可能只有单侧）。</summary>
        public bool hasRightHand = true;

        /// <summary>切换空窗期长度（ticks，默认30≈0.5秒）。</summary>
        public int switchCooldownTicks = 30;

        /// <summary>玩家是否可以装载/卸载芯片（BORDER=true，近界/黑=false）。</summary>
        public bool allowChipManagement = true;

        /// <summary>装备时是否自动激活预装芯片（近界/黑触发器用）。</summary>
        public bool autoActivateOnEquip = false;

        /// <summary>
        /// 特殊槽位数量（默认0=无特殊槽）。
        /// 特殊槽全部同时激活/关闭，不参与左右切换逻辑。
        /// 近界/黑触发器：LeftHand(0)+RightHand(0)+Special(N)。
        /// v2.1新增（T29）。
        /// </summary>
        public int specialSlotCount = 2;

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
        public SlotSide side = SlotSide.LeftHand;
        public int slotIndex = 0;
        public ThingDef chipDef;
    }
}
