namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片定义层可声明的表达条目种类。
    /// </summary>
    public enum ChipExpressionEntryKindConfig
    {
        /// <summary>
        /// 作者声明这是一条主 Verb。
        /// </summary>
        PrimaryVerb = 0,

        /// <summary>
        /// 作者声明这是一条副 Verb。
        /// </summary>
        SecondaryVerb = 1,

        /// <summary>
        /// 条目属于 Ability 通道。
        /// </summary>
        Ability = 2,

        /// <summary>
        /// 条目属于 Hediff 通道。
        /// </summary>
        Hediff = 3,

        /// <summary>
        /// 条目属于 Passive 通道。
        /// </summary>
        Passive = 4
    }
}
