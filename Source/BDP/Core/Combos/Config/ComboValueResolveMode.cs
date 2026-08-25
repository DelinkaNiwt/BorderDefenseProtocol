namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技同义字段的最小正式求值模式。
    /// 它只回答“怎么从来源芯片取值”，不回答业务要做什么。
    /// </summary>
    public enum ComboValueResolveMode
    {
        /// <summary>
        /// 跟随第一来源项。
        /// </summary>
        FollowFirstSource,

        /// <summary>
        /// 跟随第二来源项。
        /// </summary>
        FollowSecondSource,

        /// <summary>
        /// 两侧求和。
        /// </summary>
        Sum,

        /// <summary>
        /// 两侧求平均。
        /// </summary>
        Average,

        /// <summary>
        /// 两侧取最大值。
        /// </summary>
        Max,

        /// <summary>
        /// 两侧取最小值。
        /// </summary>
        Min
    }
}
