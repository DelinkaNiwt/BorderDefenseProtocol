namespace BDP.Core.Expressions
{
    /// <summary>
    /// 高层合成结果的种类。
    /// 它只区分“这是什么高层结果”，不回答具体成立规则。
    /// </summary>
    internal enum CompositeExpressionKind
    {
        /// <summary>
        /// 非高层合成结果。
        /// </summary>
        None,

        /// <summary>
        /// 双武器类高层结果。
        /// </summary>
        DualWeapon,

        /// <summary>
        /// 组合技类高层结果。
        /// </summary>
        Combo,

        /// <summary>
        /// 非攻击联动类高层结果。
        /// </summary>
        NonCombatComposite
    }
}
