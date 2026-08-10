namespace BDP.Core.Expressions
{
    /// <summary>
    /// 武器类表达的基础模式。
    /// 它只回答近战 / 远程这类最上层分型，不回答更细玩法。
    /// </summary>
    internal enum WeaponExpressionMode
    {
        /// <summary>
        /// 当前结果不是武器类，或当前阶段尚未给出武器模式。
        /// </summary>
        None,

        /// <summary>
        /// 近战武器类表达。
        /// </summary>
        Melee,

        /// <summary>
        /// 远程武器类表达。
        /// </summary>
        Ranged
    }
}
