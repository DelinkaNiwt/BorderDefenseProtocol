namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片表达条目的轻量关系种类配置枚举。
    /// </summary>
    public enum ChipExpressionRelationKindConfig
    {
        /// <summary>
        /// 条目独立存在，不挂接父条目。
        /// </summary>
        Independent = 0,

        /// <summary>
        /// 条目挂接在另一条条目之下。
        /// </summary>
        Attached = 1
    }
}
