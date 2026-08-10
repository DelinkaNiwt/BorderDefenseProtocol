using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Combos;
using Verse;

namespace BDP.Core.Expressions.Runtime
{
    /// <summary>
    /// 组合技运行时索引。
    /// 它把无序双芯片键索引到唯一 ComboDef，避免每次匹配都线性扫全部 Def。
    /// </summary>
    internal sealed class ComboRuntimeIndex
    {
        /// <summary>
        /// 当前已建立的无序双芯片索引。
        /// </summary>
        private readonly Dictionary<string, ComboDef> combosByUnorderedPair =
            new Dictionary<string, ComboDef>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 上次建索引时观测到的 ComboDef 数量。
        /// </summary>
        private int indexedComboCount = -1;

        /// <summary>
        /// 按两枚芯片 Thing 匹配唯一命中的组合技。
        /// 芯片身份优先取制造预设 defName，回退到 ThingDef.defName。
        /// </summary>
        internal ComboDefinitionReadResult FindMatch(
            Thing chipA,
            Thing chipB,
            Func<ComboDef, ComboDefinitionReadResult> read)
        {
            if (chipA == null || chipB == null)
            {
                return null;
            }

            EnsureIndex();

            string identityA = GetComboIdentity(chipA);
            string identityB = GetComboIdentity(chipB);

            if (string.IsNullOrWhiteSpace(identityA) || string.IsNullOrWhiteSpace(identityB))
            {
                return null;
            }

            ComboDef comboDef;
            if (!combosByUnorderedPair.TryGetValue(
                    BuildUnorderedPairKey(identityA, identityB),
                    out comboDef))
            {
                return null;
            }

            ComboDefinitionReadResult readResult = read != null ? read(comboDef) : null;
            return readResult != null
                && readResult.Validation != null
                && readResult.Validation.IsValid
                ? readResult
                : null;
        }

        /// <summary>
        /// 确保当前索引与已加载 ComboDef 集合保持同步。
        /// </summary>
        private void EnsureIndex()
        {
            int comboCount = DefDatabase<ComboDef>.AllDefsListForReading.Count;
            if (indexedComboCount == comboCount && combosByUnorderedPair.Count > 0)
            {
                return;
            }

            combosByUnorderedPair.Clear();
            List<ComboDef> allDefs = DefDatabase<ComboDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ComboDef comboDef = allDefs[i];
                if (comboDef == null
                    || string.IsNullOrWhiteSpace(comboDef.chipA)
                    || string.IsNullOrWhiteSpace(comboDef.chipB))
                {
                    continue;
                }

                string key = BuildUnorderedPairKey(comboDef.chipA, comboDef.chipB);
                if (!combosByUnorderedPair.ContainsKey(key))
                {
                    combosByUnorderedPair.Add(key, comboDef);
                }
            }

            indexedComboCount = comboCount;
        }

        /// <summary>
        /// 从芯片 Thing 提取 Combo 匹配用身份键。
        /// 取制造来源首个预设 defName 作为芯片的唯一身份。
        /// </summary>
        private static string GetComboIdentity(Thing chip)
        {
            ChipSourceReferenceSnapshot source = ChipInstanceSurfaceAccess.ReadSourceReference(chip);
            if (source.OrderedSourceKeys != null && source.OrderedSourceKeys.Count > 0)
            {
                return source.OrderedSourceKeys[0];
            }

            return null;
        }

        /// <summary>
        /// 为两枚芯片 DefName 生成稳定无序键。
        /// </summary>
        private static string BuildUnorderedPairKey(string firstDefName, string secondDefName)
        {
            if (string.IsNullOrWhiteSpace(firstDefName) || string.IsNullOrWhiteSpace(secondDefName))
            {
                return string.Empty;
            }

            return string.Compare(firstDefName, secondDefName, StringComparison.OrdinalIgnoreCase) <= 0
                ? firstDefName + "|" + secondDefName
                : secondDefName + "|" + firstDefName;
        }
    }
}
