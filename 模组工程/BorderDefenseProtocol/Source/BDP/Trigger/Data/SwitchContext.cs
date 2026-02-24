using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 切换阶段枚举（v6.0：三态，替代旧的二态SwitchState）。
    /// Idle=空闲，WindingDown=后摇（旧芯片仍isActive），WarmingUp=前摇（等待激活新芯片）。
    /// </summary>
    public enum SwitchPhase { Idle, WindingDown, WarmingUp }

    /// <summary>
    /// 按侧独立的切换上下文（v6.0重写）。
    /// 每侧（左/右手）各持有一个SwitchContext实例（null=Idle）。
    /// 包含当前阶段、阶段结束tick、目标槽位、后摇旧槽位、以及用于进度计算的总时长。
    /// </summary>
    public class SwitchContext : IExposable
    {
        /// <summary>当前阶段。</summary>
        public SwitchPhase phase;

        /// <summary>当前阶段结束的游戏tick。</summary>
        public int phaseEndTick;

        /// <summary>待激活的目标槽位索引。</summary>
        public int targetSlotIndex;

        /// <summary>正在后摇的旧槽位索引（仅WindingDown阶段有效，其他阶段为-1）。</summary>
        public int windingDownSlotIndex = -1;

        /// <summary>前摇总时长（tick），用于计算WarmingUp阶段进度百分比。</summary>
        public int warmupDuration;

        /// <summary>后摇总时长（tick），用于计算WindingDown阶段进度百分比。</summary>
        public int winddownDuration;

        public SwitchContext() { }

        public void ExposeData()
        {
            Scribe_Values.Look(ref phase, "phase");
            Scribe_Values.Look(ref phaseEndTick, "phaseEndTick");
            Scribe_Values.Look(ref targetSlotIndex, "targetSlotIndex");
            Scribe_Values.Look(ref windingDownSlotIndex, "windingDownSlotIndex", -1);
            Scribe_Values.Look(ref warmupDuration, "warmupDuration");
            Scribe_Values.Look(ref winddownDuration, "winddownDuration");
        }
    }
}
