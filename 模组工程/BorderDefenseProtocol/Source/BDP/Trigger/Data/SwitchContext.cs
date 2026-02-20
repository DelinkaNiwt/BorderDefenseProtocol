using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 切换状态枚举。
    /// </summary>
    public enum SwitchState { Idle, Switching }

    /// <summary>
    /// 切换空窗期的上下文数据——记录待激活的目标槽位和冷却到期tick。
    /// </summary>
    public class SwitchContext : IExposable
    {
        public SlotSide side;
        public int slotIndex;
        public int cooldownTick;

        public SwitchContext() { }

        public SwitchContext(SlotSide side, int slotIndex, int cooldownTick)
        {
            this.side = side;
            this.slotIndex = slotIndex;
            this.cooldownTick = cooldownTick;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref side, "side");
            Scribe_Values.Look(ref slotIndex, "slotIndex");
            Scribe_Values.Look(ref cooldownTick, "cooldownTick");
        }
    }
}
