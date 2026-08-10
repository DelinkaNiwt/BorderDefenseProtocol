namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片配置层使用的 Verb 基础模式枚举。
    /// 它属于上游声明契约，不等于运行时结果对象里的内部模式枚举。
    /// </summary>
    public enum VerbExpressionModeConfig
    {
        /// <summary>
        /// 当前来源不是武器类，或当前配置尚未给出武器模式。
        /// </summary>
        None,

        /// <summary>
        /// 当前来源声明为近战武器类。
        /// </summary>
        Melee,

        /// <summary>
        /// 当前来源声明为远程武器类。
        /// </summary>
        Ranged
    }
}
