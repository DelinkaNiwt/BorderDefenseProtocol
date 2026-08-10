namespace BDP.Core.CombatModel
{
    /// <summary>
    /// 双侧复合攻击的正式调度方式。
    /// 它只描述主副两侧如何排布，不承担运行时推进。
    /// </summary>
    public enum DualExecutionSchedule
    {
        /// <summary>
        /// 未声明正式双侧调度。
        /// </summary>
        None,

        /// <summary>
        /// 双侧交替执行。
        /// </summary>
        Alternating,

        /// <summary>
        /// 双侧同批并列执行。
        /// </summary>
        Simultaneous,

        /// <summary>
        /// 主侧完整执行后再副侧。
        /// </summary>
        MainThenSub,

        /// <summary>
        /// 双侧节奏不同，按混合节奏执行。
        /// </summary>
        MixedRhythm
    }
}
