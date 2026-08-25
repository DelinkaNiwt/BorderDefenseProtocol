using Verse;

namespace BDP.Content.RangedModules.Debuff
{
    /// <summary>
    /// 铅块负重的命中叠加与延迟恢复配置。
    /// </summary>
    public sealed class HediffCompProperties_StackingRecovery : HediffCompProperties
    {
        /// <summary>
        /// 每次有效命中的严重度增量。
        /// </summary>
        public float severityPerHit = 0.1f;

        /// <summary>
        /// 严重度上限。
        /// </summary>
        public float maximumSeverity = 1f;

        /// <summary>
        /// 最后一次命中后等待多少 tick 才开始恢复。
        /// </summary>
        public int recoveryDelayTicks = 300;

        /// <summary>
        /// 每次恢复间隔的 tick 数。
        /// </summary>
        public int recoveryIntervalTicks = 60;

        /// <summary>
        /// 每次恢复的严重度。
        /// </summary>
        public float severityRecoveryPerInterval = 0.01f;

        /// <summary>
        /// 绑定实际运行时组件类型。
        /// </summary>
        public HediffCompProperties_StackingRecovery()
        {
            compClass = typeof(HediffComp_StackingRecovery);
        }
    }
}
