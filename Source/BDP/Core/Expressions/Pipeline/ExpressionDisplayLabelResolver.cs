using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 统一解析最终表达结果的动作显示名称。
    /// 它只处理动态前缀，不拼接来源变体后缀。
    /// </summary>
    internal static class ExpressionDisplayLabelResolver
    {
        /// <summary>
        /// 解析当前结果在玩家入口前应显示的动作名称。
        /// </summary>
        internal static string Resolve(FormalExpressionResult result)
        {
            string label = result != null ? result.DisplayLabel : null;
            IReadOnlyList<string> prefixes = result != null ? result.DisplayLabelPrefixes : null;
            if (prefixes == null || prefixes.Count == 0)
            {
                return label;
            }

            for (int index = prefixes.Count - 1; index >= 0; index--)
            {
                string prefix = prefixes[index];
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    label = prefix + label;
                }
            }

            return label;
        }
    }
}
