namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一档表达持续 Trion 总费用。
    /// 作者按最终有效来源数，从 1 开始连续配置。
    /// </summary>
    public sealed class ExpressionSustainCostBySourceCountConfig
    {
        /// <summary>
        /// 命中本档所需的最终有效来源数量。
        /// </summary>
        public int SourceCount;

        /// <summary>
        /// 该数量下整个最终效果每秒合计消耗的 Trion。
        /// </summary>
        public float TotalPerSecond;
    }
}
