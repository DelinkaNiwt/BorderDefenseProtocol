namespace BDP.Core.CombatModel
{
    /// <summary>
    /// 远程攻击的单侧执行节奏。
    /// 它只声明一次攻击内部如何组织发射，不承担运行时推进。
    /// </summary>
    public enum RangedExecutionRhythm
    {
        /// <summary>
        /// 未声明正式远程节奏。
        /// </summary>
        None,

        /// <summary>
        /// 按顺序逐发执行。
        /// </summary>
        Sequential,

        /// <summary>
        /// 按同批齐射执行。
        /// </summary>
        Simultaneous
    }
}
