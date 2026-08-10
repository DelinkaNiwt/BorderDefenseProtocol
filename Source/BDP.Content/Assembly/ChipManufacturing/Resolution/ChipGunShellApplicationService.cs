using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Expressions;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>把可选枪壳作为最后一层覆盖一次性施加到成品配置。</summary>
    public static class ChipGunShellApplicationService
    {
        /// <summary>对所有适用的远程表达条目施加一次枪壳覆盖。</summary>
        public static void Apply(ChipDefinitionConfig config, ChipGunShellDef gunShell)
        {
            if (config?.Expression == null || gunShell == null)
            {
                return;
            }

            IList<ChipExpressionEntryConfig> entries = config.Expression.Entries;
            config.Expression.Entries = entries != null
                ? ChipGunShellExpressionService.MergeEntries(
                    new List<ChipExpressionEntryConfig>(entries),
                    gunShell,
                    null)
                : new List<ChipExpressionEntryConfig>();
        }
    }
}
