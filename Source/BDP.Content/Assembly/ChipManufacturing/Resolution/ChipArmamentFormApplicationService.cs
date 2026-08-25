using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Expressions;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>把可选武装型作为最后一层覆盖一次性施加到成品配置。</summary>
    public static class ChipArmamentFormApplicationService
    {
        /// <summary>对所有适用的武装表达条目施加一次武装型覆盖。</summary>
        public static void Apply(ChipDefinitionConfig config, ChipArmamentFormDef armamentForm)
        {
            if (config?.Expression == null || armamentForm == null)
            {
                return;
            }

            IList<ChipExpressionEntryConfig> entries = config.Expression.Entries;
            config.Expression.Entries = entries != null
                ? ChipArmamentFormExpressionService.MergeEntries(
                    new List<ChipExpressionEntryConfig>(entries),
                    armamentForm,
                    null)
                : new List<ChipExpressionEntryConfig>();
        }
    }
}
