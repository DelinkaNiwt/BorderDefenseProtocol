using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 槽位侧别枚举。
    /// v2.0变更（T23）：Main/Sub改为Left/Right，为战斗体部位系统做准备。
    /// v2.1变更（T29）：新增Special，用于内置系统芯片（全部同时激活/关闭）。
    ///   · Special：运行时槽位侧别，不参与左右切换状态机
    /// v3.0变更：Left/Right → LeftHand/RightHand，规范化三侧槽位命名
    /// </summary>
    public enum SlotSide { LeftHand, RightHand, Special }

    /// <summary>
    /// 单个芯片槽位的数据容器（非ThingComp）。
    /// 实现IExposable以支持Scribe_Collections.Look(LookMode.Deep)序列化。
    /// </summary>
    public class ChipSlot : IExposable
    {
        public int index;
        public SlotSide side;

        /// <summary>已装载的芯片物品（null=空槽）。</summary>
        public Thing loadedChip;

        /// <summary>该槽位的芯片是否当前激活。</summary>
        public bool isActive;

        // 无参构造——Activator.CreateInstance和Scribe反序列化需要
        public ChipSlot() { }

        public ChipSlot(int index, SlotSide side)
        {
            this.index = index;
            this.side = side;
            this.loadedChip = null;
            this.isActive = false;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref index, "index");
            Scribe_Values.Look(ref side, "side");
            Scribe_Deep.Look(ref loadedChip, "loadedChip");
            Scribe_Values.Look(ref isActive, "isActive");

            // 读档后校验不变量⑥：isActive=true时loadedChip必须非null
            if (Scribe.mode == LoadSaveMode.PostLoadInit && loadedChip == null)
                isActive = false;
        }

        public override string ToString()
            => $"[{side}#{index} chip={loadedChip?.LabelShortCap ?? "empty"} active={isActive}]";
    }
}
