namespace BDP.Core.Expressions
{
    /// <summary>
    /// Special 武器类唯一化裁定结果。
    /// 它只记录裁定后的三侧武器结果与是否发生拦截。
    /// </summary>
    internal sealed class SpecialWeaponOverrideResolution
    {
        /// <summary>
        /// 裁定后的 Main 结果集合。
        /// </summary>
        public SingleSideExpressionSet MainSet { get; set; }

        /// <summary>
        /// 裁定后的 Sub 结果集合。
        /// </summary>
        public SingleSideExpressionSet SubSet { get; set; }

        /// <summary>
        /// 裁定后的 Special 结果集合。
        /// </summary>
        public SingleSideExpressionSet SpecialSet { get; set; }

        /// <summary>
        /// 当前是否发生了 Special 对 Main / Sub 武器类的拦截。
        /// </summary>
        public bool HasSpecialWeaponOverride { get; set; }
    }
}
