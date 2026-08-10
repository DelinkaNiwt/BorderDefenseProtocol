using System;
using System.Collections.Generic;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.Expressions.Runtime
{
    /// <summary>
    /// 表达契约解释缓存。
    /// 它只缓存“芯片定义 + modeKey”对应的静态解释结果，不缓存最终表达是否成立。
    /// </summary>
    internal sealed class ExpressionContractCache
    {
        /// <summary>
        /// 缓存 modeKey 为空时使用的稳定占位键。
        /// </summary>
        private const string NullModeKey = "__bdp_null_mode__";

        /// <summary>
        /// 当前已缓存的正式契约解释结果。
        /// </summary>
        private readonly Dictionary<ThingDef, Dictionary<string, ChipExpressionResolvedContract>> cache =
            new Dictionary<ThingDef, Dictionary<string, ChipExpressionResolvedContract>>();

        /// <summary>
        /// 按芯片定义和 modeKey 读取或创建正式契约解释结果。
        /// </summary>
        internal ChipExpressionResolvedContract GetOrAdd(
            ThingDef thingDef,
            string modeKey,
            Func<ChipExpressionResolvedContract> factory)
        {
            if (thingDef == null)
            {
                return factory != null ? factory() : null;
            }

            Dictionary<string, ChipExpressionResolvedContract> perDefCache;
            if (!cache.TryGetValue(thingDef, out perDefCache))
            {
                perDefCache = new Dictionary<string, ChipExpressionResolvedContract>(StringComparer.OrdinalIgnoreCase);
                cache.Add(thingDef, perDefCache);
            }

            string resolvedModeKey = string.IsNullOrWhiteSpace(modeKey) ? NullModeKey : modeKey;
            ChipExpressionResolvedContract cachedResult;
            if (perDefCache.TryGetValue(resolvedModeKey, out cachedResult))
            {
                return cachedResult;
            }

            ChipExpressionResolvedContract created = factory != null ? factory() : null;
            perDefCache[resolvedModeKey] = created;
            return created;
        }
    }
}
