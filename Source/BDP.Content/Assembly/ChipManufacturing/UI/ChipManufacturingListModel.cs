using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Core.Chips;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>为页签提供固定排序的分类、职业、武装构型和芯片功能模板列表。</summary>
    public static class ChipManufacturingListModel
    {
        /// <summary>四主分类固定显示顺序。</summary>
        private static readonly string[] CategoryOrder =
        {
            "BDP_ChipCategory_Weapon",
            "BDP_ChipCategory_Defense",
            "BDP_ChipCategory_GeneralSkill",
            "BDP_ChipCategory_Accessory"
        };

        /// <summary>职业分页固定显示顺序；射手键代表射手／枪手合页。</summary>
        private static readonly string[] ProfessionOrder =
        {
            "BDP_ChipProfession_Attacker",
            "BDP_ChipProfession_Shooter",
            "BDP_ChipProfession_Sniper"
        };

        /// <summary>读取当前存在的四主分类，并保持设计顺序。</summary>
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

        /// <summary>读取三个职业分页；射手和枪手共用射手分页。</summary>
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

        /// <summary>把枪手成品职业映射到射手／枪手共用的编辑分页。</summary>
        public static ChipProfessionDef GetProfessionPage(ChipProfessionDef profession)
        {
            return profession?.defName == "BDP_ChipProfession_Gunner"
                ? ChipManufacturingDefLookup.FindProfession("BDP_ChipProfession_Shooter")
                : profession;
        }

        /// <summary>按主分类筛选芯片功能模板；所有主分类统一应用职业接纳规则。</summary>
        public static List<ChipManufacturingPresetDef> GetChipFunctionDefs(
            ChipCategoryDef category,
            ChipProfessionDef profession,
            ChipArmamentFormDef armamentForm = null)
        {
            List<ChipManufacturingPresetDef> result = new List<ChipManufacturingPresetDef>();
            List<ChipManufacturingPresetDef> all =
                DefDatabase<ChipManufacturingPresetDef>.AllDefsListForReading;
            for (int index = 0; index < all.Count; index++)
            {
                ChipManufacturingPresetDef function = all[index];
                if (function != null
                    && function.showInManufacturing
                    && function.ResearchFinished
                    && function.config?.Profile?.Category == category
                    && ChipCombinationSelectionRules.CanUseFunction(profession, function)
                    && ChipCombinationSelectionRules.CanUseArmamentFormFunction(armamentForm, function))
                {
                    result.Add(function);
                }
            }

            return result;
        }

        /// <summary>读取某个主分类下的芯片功能模板总数，不应用职业筛选。</summary>
        public static int GetFunctionCount(ChipCategoryDef category)
        {
            if (category == null)
            {
                return 0;
            }

            int count = 0;
            List<ChipManufacturingPresetDef> all =
                DefDatabase<ChipManufacturingPresetDef>.AllDefsListForReading;
            for (int index = 0; index < all.Count; index++)
            {
                ChipManufacturingPresetDef function = all[index];
                if (function != null
                    && function.showInManufacturing
                    && function.ResearchFinished
                    && function.config?.Profile?.Category == category)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>先选功能再列出合法构型；合页构型按枪手职业校验，隐藏默认型不进入制造台。</summary>
        public static List<ChipArmamentFormDef> GetArmamentForms(
            ChipCategoryDef category,
            ChipProfessionDef profession,
            IList<ChipManufacturingPresetDef> selectedFunctions = null)
        {
            List<ChipArmamentFormDef> result = new List<ChipArmamentFormDef>();
            if (category == null || category.defName != "BDP_ChipCategory_Weapon" || profession == null
                || selectedFunctions == null || selectedFunctions.Count == 0)
            {
                return result;
            }

            ChipProfessionDef formProfession = profession.defName == "BDP_ChipProfession_Shooter"
                ? ChipManufacturingDefLookup.FindProfession("BDP_ChipProfession_Gunner")
                : profession;

            List<ChipArmamentFormDef> all =
                DefDatabase<ChipArmamentFormDef>.AllDefsListForReading;
            for (int index = 0; index < all.Count; index++)
            {
                if (all[index] != null
                    && all[index].showInManufacturing
                    && all[index].ResearchFinished
                    && !all[index].implicitDefault
                    && formProfession != null
                    && all[index].compatibleProfessions != null
                    && all[index].compatibleProfessions.Contains(formProfession)
                    && ChipCombinationCompatibilityService.Validate(
                        category, formProfession, selectedFunctions, all[index]).Count == 0)
                {
                    result.Add(all[index]);
                }
            }

            return result;
        }

        /// <summary>在草稿副本上检查点击芯片功能模板是否可成立，不污染真实选择。</summary>
        public static bool CanSelectFunction(
            ChipManufacturingDraft draft,
            ChipProfessionDef profession,
            ChipManufacturingPresetDef function,
            out string failureCode)
        {
            BDP.Content.Assembly.ChipManufacturing.Model.ChipCombinationRecord copy =
                draft.Record.Clone();
            return ChipCombinationSelectionRules.TrySelect(
                copy,
                profession,
                function,
                out failureCode);
        }
    }
}
