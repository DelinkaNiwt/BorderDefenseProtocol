using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Model
{
    /// <summary>
    /// 一枚芯片组合只保存玩家选择，不保存任何可重新计算的结果。
    /// </summary>
    public sealed class ChipCombinationRecord : IExposable
    {
        /// <summary>唯一主分类 DefName。</summary>
        public string CategoryDefName;

        /// <summary>可空且唯一的最终职业 DefName。</summary>
        public string ProfessionDefName;

        /// <summary>顺序敏感的芯片功能模板 DefName。</summary>
        public List<string> OrderedChipFunctionDefNames = new List<string>();

        /// <summary>可空的显式武装型 DefName。</summary>
        public string ArmamentFormDefName;

        /// <summary>来源缺失时用于展示的最后成功名称。</summary>
        public string LastResolvedLabel;

        /// <summary>保存与读取组合选择。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref CategoryDefName, "categoryDefName");
            Scribe_Values.Look(ref ProfessionDefName, "professionDefName");
            Scribe_Collections.Look(ref OrderedChipFunctionDefNames, "orderedChipFunctionDefNames", LookMode.Value);
            Scribe_Values.Look(ref ArmamentFormDefName, "armamentFormDefName");
            Scribe_Values.Look(ref LastResolvedLabel, "lastResolvedLabel");

            if (OrderedChipFunctionDefNames == null)
            {
                OrderedChipFunctionDefNames = new List<string>();
            }
        }

        /// <summary>深复制当前选择记录。</summary>
        public ChipCombinationRecord Clone()
        {
            return new ChipCombinationRecord
            {
                CategoryDefName = CategoryDefName,
                ProfessionDefName = ProfessionDefName,
                OrderedChipFunctionDefNames = OrderedChipFunctionDefNames != null
                    ? new List<string>(OrderedChipFunctionDefNames)
                    : new List<string>(),
                ArmamentFormDefName = ArmamentFormDefName,
                LastResolvedLabel = LastResolvedLabel
            };
        }

        /// <summary>
        /// 比较决定制造身份的字段；芯片功能模板顺序敏感，最后成功标签不参与。
        /// </summary>
        public bool SameConfigurationAs(ChipCombinationRecord other)
        {
            if (other == null
                || !string.Equals(CategoryDefName, other.CategoryDefName, StringComparison.Ordinal)
                || !string.Equals(ProfessionDefName, other.ProfessionDefName, StringComparison.Ordinal)
                || !string.Equals(ArmamentFormDefName, other.ArmamentFormDefName, StringComparison.Ordinal))
            {
                return false;
            }

            int count = OrderedChipFunctionDefNames != null ? OrderedChipFunctionDefNames.Count : 0;
            int otherCount = other.OrderedChipFunctionDefNames != null
                ? other.OrderedChipFunctionDefNames.Count
                : 0;
            if (count != otherCount)
            {
                return false;
            }

            for (int index = 0; index < count; index++)
            {
                if (!string.Equals(
                    OrderedChipFunctionDefNames[index],
                    other.OrderedChipFunctionDefNames[index],
                    StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
