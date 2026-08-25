using System.Collections.Generic;
using BDP.Core.Combos;

namespace BDP.Core.Expressions.External
{
    /// <summary>
    /// 组合条目来源变体修正提供器注册表。
    /// 当前只保留一个正式提供器，重复注册时以最新实例为准。
    /// </summary>
    public static class ComboExpressionVariantModifierRegistry
    {
        /// <summary>
        /// 当前注册的组合条目来源变体修正提供器。
        /// </summary>
        private static IComboExpressionVariantModifierProvider provider;

        /// <summary>
        /// 注册组合条目来源变体修正提供器。
        /// </summary>
        public static void Register(IComboExpressionVariantModifierProvider nextProvider)
        {
            provider = nextProvider;
        }

        /// <summary>
        /// 对组合条目副本应用当前来源变体修正提供器。
        /// 未注册提供器时保持 Core 原始行为。
        /// </summary>
        public static void Apply(
            IList<ComboExpressionEntryConfig> comboEntries,
            string sourceVariantKey)
        {
            if (provider == null || comboEntries == null)
            {
                return;
            }

            provider.Apply(comboEntries, sourceVariantKey);
        }
    }
}
