namespace BDP.Core.CombatModel
{
    /// <summary>
    /// 近战攻击的单侧执行节奏。
    /// 它只声明一次攻击内部如何组织命中，不承担运行时推进。
    /// </summary>
    public enum MeleeExecutionRhythm
    {
        /// <summary>
        /// 未声明正式近战节奏。
        /// </summary>
        None,

        /// <summary>
        /// 一次攻击只打一击。
        /// </summary>
        SingleHit,

        /// <summary>
        /// 一次攻击会展开多击。
        /// </summary>
        MultiHit
    }
}
