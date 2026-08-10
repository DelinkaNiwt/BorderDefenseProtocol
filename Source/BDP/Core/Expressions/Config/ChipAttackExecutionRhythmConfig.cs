namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片 Def 层可写的统一攻击节奏枚举。
    /// 它是作者接口，不直接等于内部正式模型枚举。
    /// </summary>
    public enum ChipAttackExecutionRhythmConfig
    {
        /// <summary>
        /// 未显式声明节奏。
        /// 解释器会按武器模式补默认值。
        /// </summary>
        None,

        /// <summary>
        /// 常规节奏。
        /// 近战当前统一只保留这一种作者侧模式，单段还是多段由 HitCount 决定。
        /// </summary>
        Normal,

        /// <summary>
        /// 逐发。
        /// 仅适用于远程作者写法。
        /// </summary>
        Sequential,

        /// <summary>
        /// 齐射。
        /// 仅适用于远程作者写法。
        /// </summary>
        Simultaneous
    }
}
