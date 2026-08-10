namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达结果的来源关系类型。
    /// 它用于区分结果是单侧成立，还是高层重算后产生。
    /// </summary>
    internal enum ExpressionOriginKind
    {
        /// <summary>
        /// Main 单侧结果。
        /// </summary>
        Main,

        /// <summary>
        /// Sub 单侧结果。
        /// </summary>
        Sub,

        /// <summary>
        /// Special 单侧结果。
        /// </summary>
        Special,

        /// <summary>
        /// Main / Sub 高层重算后形成的组合结果。
        /// </summary>
        Composite
    }
}
