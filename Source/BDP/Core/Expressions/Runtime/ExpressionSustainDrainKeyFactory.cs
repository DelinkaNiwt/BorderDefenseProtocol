using BDP.Core.Trion;

namespace BDP.Core.Expressions.Runtime
{
    /// <summary>
    /// 为最终表达效果生成稳定的 Trion 持续消耗账本键。
    /// 键的身份与实际宿主投影对齐，不使用玩家可见名称。
    /// </summary>
    internal static class ExpressionSustainDrainKeyFactory
    {
        /// <summary>
        /// 按最终表达种类和实际效果身份生成账本键。
        /// </summary>
        internal static TrionDrainKey Create(FormalExpressionResult result)
        {
            ExpressionResultKind resultKind = result != null
                ? result.ResultKind
                : ExpressionResultKind.Passive;
            return new TrionDrainKey(
                "Expression",
                resultKind.ToString(),
                -1,
                ResolveStableEffectIdentity(result));
        }

        /// <summary>
        /// 解析与正式宿主一致的效果身份。
        /// Ability、Hediff 与 Passive 会按共享宿主合并；Verb 保留各自攻击入口身份。
        /// </summary>
        private static string ResolveStableEffectIdentity(FormalExpressionResult result)
        {
            if (result == null)
            {
                return "missing";
            }

            switch (result.ResultKind)
            {
                case ExpressionResultKind.Ability:
                    return ResolveOrFallback(result.AbilityDefName, result.Id);
                case ExpressionResultKind.Hediff:
                    return ResolveOrFallback(result.HediffDefName, result.Id);
                case ExpressionResultKind.Passive:
                    return ResolveOrFallback(result.PassiveKey, result.Id);
                case ExpressionResultKind.Verb:
                    if (!string.IsNullOrWhiteSpace(result.ComboDefName))
                    {
                        return "combo:" + result.ComboDefName + ":" + ResolveOrFallback(result.Id, "entry");
                    }

                    string chipDefName = result.SourceReference != null
                        ? result.SourceReference.ChipDefName
                        : null;
                    return "chip:"
                        + ResolveOrFallback(chipDefName, "unknown")
                        + ":"
                        + ResolveOrFallback(result.Id, "entry");
                default:
                    return ResolveOrFallback(result.Id, "unknown");
            }
        }

        /// <summary>
        /// 返回首选稳定值；首选为空时使用明确兜底。
        /// </summary>
        private static string ResolveOrFallback(string preferred, string fallback)
        {
            return !string.IsNullOrWhiteSpace(preferred)
                ? preferred
                : (!string.IsNullOrWhiteSpace(fallback) ? fallback : "unknown");
        }
    }
}
