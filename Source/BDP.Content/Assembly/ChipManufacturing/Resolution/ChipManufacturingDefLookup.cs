using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 芯片制造 DefName 到当前 Def 的集中查找入口。
    /// </summary>
    public static class ChipManufacturingDefLookup
    {
        /// <summary>查找主分类。</summary>
        public static ChipCategoryDef FindCategory(string defName)
        {
            return string.IsNullOrWhiteSpace(defName)
                ? null
                : DefDatabase<ChipCategoryDef>.GetNamedSilentFail(defName);
        }

        /// <summary>查找职业。</summary>
        public static ChipProfessionDef FindProfession(string defName)
        {
            return string.IsNullOrWhiteSpace(defName)
                ? null
                : DefDatabase<ChipProfessionDef>.GetNamedSilentFail(defName);
        }

        /// <summary>查找动作预设。</summary>
        public static ChipActionPresetDef FindAction(string defName)
        {
            return string.IsNullOrWhiteSpace(defName)
                ? null
                : DefDatabase<ChipActionPresetDef>.GetNamedSilentFail(defName);
        }

        /// <summary>查找武装型预设。</summary>
        public static ChipArmamentFormDef FindArmamentForm(string defName)
        {
            return string.IsNullOrWhiteSpace(defName)
                ? null
                : DefDatabase<ChipArmamentFormDef>.GetNamedSilentFail(defName);
        }

        /// <summary>
        /// 按职业和动作内容查找隐藏默认武装型。
        /// 默认型只参与逻辑解析，不参与制造台列表和动态名称。
        /// </summary>
        public static ChipArmamentFormDef FindImplicitDefaultArmamentForm(
            ChipCategoryDef category,
            ChipProfessionDef profession,
            IList<ChipActionPresetDef> actions)
        {
            if (category == null || profession == null || actions == null || actions.Count == 0)
            {
                return null;
            }

            List<ChipArmamentFormDef> all =
                DefDatabase<ChipArmamentFormDef>.AllDefsListForReading;
            for (int index = 0; index < all.Count; index++)
            {
                ChipArmamentFormDef candidate = all[index];
                if (candidate == null
                    || !candidate.implicitDefault
                    || candidate.compatibleProfessions == null
                    || !candidate.compatibleProfessions.Contains(profession))
                {
                    continue;
                }

                if (HasRangedWeaponAction(category, actions)
                    && ChipCombinationSelectionRules.CanUseArmamentForm(candidate, actions))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>判断动作集合是否含有远程武装表达条目。</summary>
        private static bool HasRangedWeaponAction(
            ChipCategoryDef category,
            IList<ChipActionPresetDef> actions)
        {
            if (category.defName != "BDP_ChipCategory_Weapon")
            {
                return false;
            }

            for (int index = 0; index < actions.Count; index++)
            {
                List<ChipExpressionEntryConfig> entries =
                    actions[index]?.config?.Expression?.Entries;
                if (entries == null)
                {
                    continue;
                }

                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    ChipExpressionEntryConfig entry = entries[entryIndex];
                    if ((entry?.Kind == ChipExpressionEntryKindConfig.PrimaryVerb
                            || entry?.Kind == ChipExpressionEntryKindConfig.SecondaryVerb)
                        && entry.WeaponMode == VerbExpressionModeConfig.Ranged)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
