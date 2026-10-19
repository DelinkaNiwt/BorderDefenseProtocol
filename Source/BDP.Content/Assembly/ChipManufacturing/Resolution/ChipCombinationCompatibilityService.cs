using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Core.Chips;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>集中判断来源齐全的芯片组合是否合法。</summary>
    public static class ChipCombinationCompatibilityService
    {
        /// <summary>返回全部稳定失败原因；空列表表示组合合法。</summary>
        public static List<ChipCombinationFailureReason> Validate(
            ChipCategoryDef category,
            ChipProfessionDef profession,
            IList<ChipManufacturingPresetDef> functionDefs,
            ChipArmamentFormDef armamentForm)
        {
            List<ChipCombinationFailureReason> failures =
                new List<ChipCombinationFailureReason>();
            int functionCount = functionDefs != null ? functionDefs.Count : 0;
            if (functionCount < 1
                || functionCount > ChipCombinationSelectionRules.MaxFunctionCount(profession, armamentForm))
            {
                Add(failures, "FunctionCount", "BDP_ChipManufacturing_InvalidFunctionCount");
                return failures;
            }

            HashSet<string> functionNames = new HashSet<string>();
            for (int index = 0; index < functionCount; index++)
            {
                ChipManufacturingPresetDef function = functionDefs[index];
                if (function == null)
                {
                    Add(failures, "MissingFunction", "BDP_ChipManufacturing_MissingFunction");
                    continue;
                }

                if (!functionNames.Add(function.defName))
                {
                    Add(failures, "DuplicateFunction", "BDP_ChipManufacturing_DuplicateFunction");
                }

                if (function.config?.Profile?.Category != category)
                {
                    Add(failures, "CategoryMismatch", "BDP_ChipManufacturing_CategoryMismatch");
                }

                if (!ChipCombinationSelectionRules.CanUseFunction(profession, function))
                {
                    Add(failures, "ProfessionMismatch", "BDP_ChipManufacturing_ProfessionMismatch");
                }

                if (functionCount > 1
                    && ChipCombinationSelectionRules.HasIntrinsicMultipleModes(function))
                {
                    Add(
                        failures,
                        "IntrinsicMultiMode",
                        "BDP_ChipManufacturing_IntrinsicMultiModeMustStandAlone");
                }
            }

            if (armamentForm != null
                && (profession == null
                    || armamentForm.compatibleProfessions == null
                    || !armamentForm.compatibleProfessions.Contains(profession)))
            {
                Add(failures, "ArmamentFormProfession", "BDP_ChipManufacturing_ArmamentFormProfessionMismatch");
            }

            if (!ChipCombinationSelectionRules.CanUseArmamentForm(armamentForm, functionDefs))
            {
                Add(failures, "ArmamentFormFunction", "BDP_ChipManufacturing_ArmamentFormFunctionMismatch");
            }

            if (functionCount == 2)
            {
                ValidateDualStructure(functionDefs[0], functionDefs[1], failures);
            }

            return failures;
        }

        /// <summary>检查双芯片功能模板必须相同的结构，以及显式可合并字段。</summary>
        private static void ValidateDualStructure(
            ChipManufacturingPresetDef first,
            ChipManufacturingPresetDef second,
            List<ChipCombinationFailureReason> failures)
        {
            ChipDefinitionConfig firstConfig = first?.config;
            ChipDefinitionConfig secondConfig = second?.config;
            if (firstConfig?.Loadout == null || secondConfig?.Loadout == null)
            {
                Add(failures, "MissingLoadout", "BDP_ChipManufacturing_MissingLoadout");
                return;
            }

            if (firstConfig.Loadout.SlotRegion != secondConfig.Loadout.SlotRegion)
            {
                Add(failures, "SlotRegionMismatch", "BDP_ChipManufacturing_SlotRegionMismatch");
            }

            if (firstConfig.Loadout.SlotOccupancy != secondConfig.Loadout.SlotOccupancy)
            {
                Add(failures, "SlotOccupancyMismatch", "BDP_ChipManufacturing_SlotOccupancyMismatch");
            }

            if (!ChipRequirementMergeRegistry.CanMerge(
                firstConfig.ActivationRequirements,
                secondConfig.ActivationRequirements))
            {
                Add(failures, "RequirementMergeRuleMissing", "BDP_ChipManufacturing_RequirementMergeRuleMissing");
            }

            if (!ChipExtensionMergeRegistry.CanMerge(
                firstConfig.Extensions,
                secondConfig.Extensions))
            {
                Add(failures, "ExtensionMergeRuleMissing", "BDP_ChipManufacturing_ExtensionMergeRuleMissing");
            }
        }

        /// <summary>按代码去重追加失败原因。</summary>
        private static void Add(
            List<ChipCombinationFailureReason> target,
            string code,
            string translationKey)
        {
            for (int index = 0; index < target.Count; index++)
            {
                if (target[index].Code == code)
                {
                    return;
                }
            }

            target.Add(new ChipCombinationFailureReason(code, translationKey));
        }
    }
}
