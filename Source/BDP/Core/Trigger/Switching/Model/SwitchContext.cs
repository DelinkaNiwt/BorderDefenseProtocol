using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 单侧切换上下文。
    /// 第一版先保留当前阶段、结束时间、目标槽位和进度计算需要的数据。
    /// </summary>
    public sealed class SwitchContext : IExposable
    {
        /// <summary>
        /// 当前表现阶段。
        /// </summary>
        public SwitchPhase phase;

        /// <summary>
        /// 当前表现阶段结束的绝对 tick。
        /// </summary>
        public int phaseEndTick;

        /// <summary>
        /// 当前准备切到哪个槽位。
        /// </summary>
        public int targetSlotIndex = -1;

        /// <summary>
        /// 当前目标芯片的稳定物品身份。
        /// 它防止等待期间槽位内容被替换后误启用另一枚芯片。
        /// </summary>
        public string targetChipThingId;

        /// <summary>
        /// 当前正在停用的是哪个槽位。
        /// </summary>
        public int deactivatingSlotIndex = -1;

        /// <summary>
        /// 启用延迟时长。
        /// </summary>
        public int activationDelayDuration;

        /// <summary>
        /// 停用延迟时长。
        /// </summary>
        public int deactivationDelayDuration;

        /// <summary>
        /// 存读档单侧切换上下文。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref phase, "phase", SwitchPhase.Idle);
            Scribe_Values.Look(ref phaseEndTick, "phaseEndTick", 0);
            Scribe_Values.Look(ref targetSlotIndex, "targetSlotIndex", -1);
            Scribe_Values.Look(ref targetChipThingId, "targetChipThingId");
            Scribe_Values.Look(ref deactivatingSlotIndex, "deactivatingSlotIndex", -1);
            Scribe_Values.Look(ref activationDelayDuration, "activationDelayDuration", 0);
            Scribe_Values.Look(ref deactivationDelayDuration, "deactivationDelayDuration", 0);
        }
    }
}
