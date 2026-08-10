namespace BDP.Core.Expressions
{
    /// <summary>
    /// 主模组正式承认的芯片表达条目轻量关系种类枚举。
    /// </summary>
    public enum ChipExpressionRelationKind
    {
        /// <summary>
        /// 条目独立存在。
        /// </summary>
        Independent = 0,

        /// <summary>
        /// 条目挂接在另一条条目之下。
        /// </summary>
        Attached = 1
    }
}
