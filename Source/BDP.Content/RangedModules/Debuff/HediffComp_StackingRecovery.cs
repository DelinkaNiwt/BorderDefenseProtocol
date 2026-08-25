using Verse;

namespace BDP.Content.RangedModules.Debuff
{
    /// <summary>
    /// 处理命中刷新、延迟恢复和严重度归零移除的 HediffComp。
    /// </summary>
    public sealed class HediffComp_StackingRecovery : HediffComp
    {
        /// <summary>
        /// 当前组件的强类型配置。
        /// </summary>
        public HediffCompProperties_StackingRecovery Props
        {
            get { return (HediffCompProperties_StackingRecovery)props; }
        }

        /// <summary>
        /// 最后一次有效命中的游戏 tick。
        /// </summary>
        private int lastHitTick = -1;

        /// <summary>
        /// 已经积累的恢复 tick。
        /// </summary>
        private int recoveryTicks;

        /// <summary>
        /// 注册一次通过目标筛选的有效命中。
        /// </summary>
        public void NotifyEffectiveHit()
        {
            lastHitTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            recoveryTicks = 0;
            if (parent != null && Props != null)
            {
                parent.Severity = UnityEngine.Mathf.Min(
                    Props.maximumSeverity,
                    parent.Severity);
            }
        }

        /// <summary>
        /// 按延迟与恢复间隔逐步降低严重度。
        /// </summary>
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            if (parent == null || Props == null || parent.Severity <= 0f)
            {
                return;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (lastHitTick < 0 || currentTick - lastHitTick <= Props.recoveryDelayTicks)
            {
                return;
            }

            recoveryTicks += delta;
            int interval = UnityEngine.Mathf.Max(1, Props.recoveryIntervalTicks);
            while (recoveryTicks >= interval)
            {
                recoveryTicks -= interval;
                severityAdjustment -= UnityEngine.Mathf.Max(0f, Props.severityRecoveryPerInterval);
            }
        }

        /// <summary>
        /// 严重度归零后请求移除当前状态。
        /// </summary>
        public override bool CompShouldRemove
        {
            get { return parent == null || parent.Severity <= 0f; }
        }

        /// <summary>
        /// 保存命中计时与恢复进度。
        /// </summary>
        public override void CompExposeData()
        {
            Scribe_Values.Look(ref lastHitTick, "lastHitTick", -1);
            Scribe_Values.Look(ref recoveryTicks, "recoveryTicks", 0);
        }
    }
}
