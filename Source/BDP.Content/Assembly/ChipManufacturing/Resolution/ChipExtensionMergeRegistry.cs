using System;
using System.Collections.Generic;
using BDP.Core.Chips;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>芯片扩展合并规则登记表；第一版不默认猜测任何扩展语义。</summary>
    public static class ChipExtensionMergeRegistry
    {
        /// <summary>显式扩展规则；当前有意保持为空。</summary>
        private static readonly List<IChipExtensionMergeRule> Rules =
            new List<IChipExtensionMergeRule>();

        /// <summary>仅当两侧扩展都为空，或所有类型已有规则时允许合并。</summary>
        public static bool CanMerge(
            IList<ChipExtensionConfig> first,
            IList<ChipExtensionConfig> second)
        {
            int firstCount = first != null ? first.Count : 0;
            int secondCount = second != null ? second.Count : 0;
            if (Rules.Count == 0)
            {
                return firstCount == 0 && secondCount == 0;
            }

            return AllSupported(first) && AllSupported(second);
        }

        /// <summary>按规则合并扩展；当前空注册表只会返回空列表。</summary>
        public static List<ChipExtensionConfig> Merge(
            IList<ChipExtensionConfig> first,
            IList<ChipExtensionConfig> second)
        {
            if (!CanMerge(first, second))
            {
                throw new InvalidOperationException("扩展未登记合并规则。");
            }

            return new List<ChipExtensionConfig>();
        }

        /// <summary>检查列表中每一种扩展是否已经登记。</summary>
        private static bool AllSupported(IList<ChipExtensionConfig> extensions)
        {
            if (extensions == null)
            {
                return true;
            }

            for (int index = 0; index < extensions.Count; index++)
            {
                ChipExtensionConfig extension = extensions[index];
                bool found = false;
                for (int ruleIndex = 0; ruleIndex < Rules.Count; ruleIndex++)
                {
                    if (extension != null
                        && Rules[ruleIndex].ExtensionType == extension.GetType())
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
