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

        /// <summary>顺序敏感的动作预设 DefName。</summary>
        public List<string> OrderedActionPresetDefNames = new List<string>();

        /// <summary>可空的枪壳 DefName。</summary>
        public string GunShellDefName;

        /// <summary>来源缺失时用于展示的最后成功名称。</summary>
        public string LastResolvedLabel;

        /// <summary>保存与读取组合选择。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref CategoryDefName, "categoryDefName");
            Scribe_Values.Look(ref ProfessionDefName, "professionDefName");
            Scribe_Collections.Look(ref OrderedActionPresetDefNames, "orderedActionPresetDefNames", LookMode.Value);
            Scribe_Values.Look(ref GunShellDefName, "gunShellDefName");
            Scribe_Values.Look(ref LastResolvedLabel, "lastResolvedLabel");
            if (OrderedActionPresetDefNames == null)
            {
                OrderedActionPresetDefNames = new List<string>();
            }
        }

        /// <summary>深复制当前选择记录。</summary>
        public ChipCombinationRecord Clone()
        {
            return new ChipCombinationRecord
            {
                CategoryDefName = CategoryDefName,
                ProfessionDefName = ProfessionDefName,
                OrderedActionPresetDefNames = OrderedActionPresetDefNames != null
                    ? new List<string>(OrderedActionPresetDefNames)
                    : new List<string>(),
                GunShellDefName = GunShellDefName,
                LastResolvedLabel = LastResolvedLabel
            };
        }

        /// <summary>
        /// 比较决定制造身份的字段；动作顺序敏感，最后成功标签不参与。
        /// </summary>
        public bool SameConfigurationAs(ChipCombinationRecord other)
        {
            if (other == null
                || !string.Equals(CategoryDefName, other.CategoryDefName, StringComparison.Ordinal)
                || !string.Equals(ProfessionDefName, other.ProfessionDefName, StringComparison.Ordinal)
                || !string.Equals(GunShellDefName, other.GunShellDefName, StringComparison.Ordinal))
            {
                return false;
            }

            int count = OrderedActionPresetDefNames != null ? OrderedActionPresetDefNames.Count : 0;
            int otherCount = other.OrderedActionPresetDefNames != null
                ? other.OrderedActionPresetDefNames.Count
                : 0;
            if (count != otherCount)
            {
                return false;
            }

            for (int index = 0; index < count; index++)
            {
                if (!string.Equals(
                    OrderedActionPresetDefNames[index],
                    other.OrderedActionPresetDefNames[index],
                    StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
