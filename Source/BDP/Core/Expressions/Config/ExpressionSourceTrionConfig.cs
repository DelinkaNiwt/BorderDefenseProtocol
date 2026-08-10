using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 单条表达来源自己的 Trion 参数区。
    /// 它属于来源级参数，不替代芯片整体级占用配置。
    /// </summary>
    public sealed class ExpressionSourceTrionConfig
    {
        /// <summary>
        /// 当前来源每次使用时消耗多少 Trion。
        /// </summary>
        public float UseCost;

        /// <summary>
        /// 当前来源成立或使用所需的最低 Trion 要求。
        /// </summary>
        public float MinimumRequired;

        /// <summary>
        /// 按最终有效来源数配置的整组表达每秒 Trion 总费用。
        /// 条目必须从 SourceCount=1 开始连续递增；超出最高档时沿用最后一档。
        /// </summary>
        public List<ExpressionSustainCostBySourceCountConfig> SustainCostBySourceCount =
            new List<ExpressionSustainCostBySourceCountConfig>();
    }
}
