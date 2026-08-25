using System.Collections.Generic;
using BDP.Core.Combos;

namespace BDP.Core.Expressions.External
{
    /// <summary>
    /// 组合条目来源变体修正提供器。
    /// Core 只表达“根据中性来源变体键修正条目副本”，不暴露任何 Content 业务类型。
    /// </summary>
    public interface IComboExpressionVariantModifierProvider
    {
        /// <summary>
        /// 在组合条目副本上应用来源变体修正。
        /// </summary>
        void Apply(
            IList<ComboExpressionEntryConfig> comboEntries,
            string sourceVariantKey);
    }
}
