using System;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口的芯片名称显示格式化器。
    /// 只处理界面文本，不修改 Thing（物品实例）或 Def（定义）的真实名称。
    /// </summary>
    internal static class ChipDisplayNameFormatter
    {
        /// <summary>移除当前语言产品标签中的固定芯片前缀。</summary>
        internal static string Format(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                return rawName ?? string.Empty;
            }

            string prefix = "BDP_ChipManufacturing_ProductLabel".Translate(string.Empty).ToString();
            if (StartsWithPrefix(rawName, prefix))
            {
                return rawName.Substring(prefix.Length);
            }

            string fullWidthColonPrefix = prefix.Replace(':', '：');
            return StartsWithPrefix(rawName, fullWidthColonPrefix)
                ? rawName.Substring(fullWidthColonPrefix.Length)
                : rawName;
        }

        /// <summary>判断名称是否以有效的产品前缀开头。</summary>
        private static bool StartsWithPrefix(string rawName, string prefix)
        {
            return !string.IsNullOrEmpty(prefix)
                && rawName.StartsWith(prefix, StringComparison.Ordinal);
        }
    }
}
