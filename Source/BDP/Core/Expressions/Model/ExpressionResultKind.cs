namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达结果的顶层类别。
    /// 这一层只回答“它属于哪一大类”，不回答更细的玩法规则。
    /// </summary>
    internal enum ExpressionResultKind
    {
        /// <summary>
        /// 通过原版 Verb 通道成立的表达。
        /// </summary>
        Verb,

        /// <summary>
        /// 通过原版 Ability 通道成立的表达。
        /// </summary>
        Ability,

        /// <summary>
        /// 通过原版 Hediff 通道成立的表达。
        /// </summary>
        Hediff,

        /// <summary>
        /// 不直接生成主动入口，但正式成立的被动表达。
        /// </summary>
        Passive
    }
}
