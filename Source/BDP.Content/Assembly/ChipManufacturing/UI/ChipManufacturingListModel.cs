using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Core.Chips;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>为页签提供固定排序的分类、职业、武装型和动作列表。</summary>
    public static class ChipManufacturingListModel
    {
        /// <summary>五主分类固定显示顺序。</summary>
        private static readonly string[] CategoryOrder =
        {
            "BDP_ChipCategory_Weapon",
            "BDP_ChipCategory_Defense",
            "BDP_ChipCategory_Ability",
            "BDP_ChipCategory_Status",
            "BDP_ChipCategory_Passive"
        };

        /// <summary>武装职业固定显示顺序。</summary>
        private static readonly string[] ProfessionOrder =
        {
            "BDP_ChipProfession_Attacker",
            "BDP_ChipProfession_Shooter",
            "BDP_ChipProfession_Gunner",
            "BDP_ChipProfession_Sniper"
        };

        /// <summary>读取当前存在的五主分类，并保持设计顺序。</summary>
        public static List<ChipCategoryDef> GetCategories()
        {
            List<ChipCategoryDef> result = new List<ChipCategoryDef>();
            for (int index = 0; index < CategoryOrder.Length; index++)
            {
                ChipCategoryDef category =
                    ChipManufacturingDefLookup.FindCategory(CategoryOrder[index]);
                if (category != null)
                {
                    result.Add(category);
                }
            }

            return result;
        }

        /// <summary>读取当前存在的四个武装职业，并保持设计顺序。</summary>
        public static List<ChipProfessionDef> GetProfessions()
        {
            List<ChipProfessionDef> result = new List<ChipProfessionDef>();
            for (int index = 0; index < ProfessionOrder.Length; index++)
            {
                ChipProfessionDef profession =
                    ChipManufacturingDefLookup.FindProfession(ProfessionOrder[index]);
                if (profession != null)
                {
                    result.Add(profession);
                }
            }

            return result;
        }

        /// <summary>按主分类筛选动作；仅武装分类继续应用职业接纳规则。</summary>
        public static List<ChipActionPresetDef> GetActions(
            ChipCategoryDef category,
            ChipProfessionDef profession,
            ChipArmamentFormDef armamentForm = null)
        {
            List<ChipActionPresetDef> result = new List<ChipActionPresetDef>();
            List<ChipActionPresetDef> all =
                DefDatabase<ChipActionPresetDef>.AllDefsListForReading;
            for (int index = 0; index < all.Count; index++)
            {
                ChipActionPresetDef action = all[index];
                if (action?.config?.Profile?.Category == category
                    && ChipCombinationSelectionRules.CanUseAction(profession, action)
                    && ChipCombinationSelectionRules.CanUseArmamentFormAction(armamentForm, action))
                {
                    result.Add(action);
                }
            }

            return result;
        }

        /// <summary>读取某个主分类下的动作预设总数，不应用职业筛选。</summary>
        public static int GetActionCount(ChipCategoryDef category)
        {
            if (category == null)
            {
                return 0;
            }

            int count = 0;
            List<ChipActionPresetDef> all =
                DefDatabase<ChipActionPresetDef>.AllDefsListForReading;
            for (int index = 0; index < all.Count; index++)
            {
                ChipActionPresetDef action = all[index];
                if (action?.config?.Profile?.Category == category)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>读取当前职业可见且可使用的武装型；隐藏默认型不进入制造台。</summary>
        public static List<ChipArmamentFormDef> GetArmamentForms(
            ChipProfessionDef profession,
            IList<ChipActionPresetDef> selectedActions = null)
        {
            List<ChipArmamentFormDef> result = new List<ChipArmamentFormDef>();
            List<ChipArmamentFormDef> all =
                DefDatabase<ChipArmamentFormDef>.AllDefsListForReading;
            for (int index = 0; index < all.Count; index++)
            {
                if (all[index] != null
                    && all[index].showInManufacturing
                    && !all[index].implicitDefault
                    && profession != null
                    && all[index].compatibleProfessions != null
                    && all[index].compatibleProfessions.Contains(profession)
                    && ChipCombinationSelectionRules.CanUseArmamentForm(
                        all[index],
                        selectedActions))
                {
                    result.Add(all[index]);
                }
            }

            return result;
        }

        /// <summary>在草稿副本上检查点击动作是否可成立，不污染真实选择。</summary>
        public static bool CanSelectAction(
            ChipManufacturingDraft draft,
            ChipProfessionDef profession,
            ChipActionPresetDef action,
            out string failureCode)
        {
            BDP.Content.Assembly.ChipManufacturing.Model.ChipCombinationRecord copy =
                draft.Record.Clone();
            return ChipCombinationSelectionRules.TrySelect(
                copy,
                profession,
                action,
                out failureCode);
        }
    }
}
