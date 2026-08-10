namespace BDP.Core.Expressions
{
    /// <summary>
    /// 主模组正式承认的表达条目种类。
    /// Verb 条目在这一层已经过正规化。
    /// </summary>
    public enum ChipExpressionEntryKind
    {
        /// <summary>
        /// 正规化后的主 Verb。
        /// </summary>
        PrimaryVerb = 0,

        /// <summary>
        /// 正规化后的副 Verb。
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
