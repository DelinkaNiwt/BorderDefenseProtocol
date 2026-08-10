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
            IList<ChipActionPresetDef> actions,
            ChipGunShellDef gunShell)
        {
            List<ChipCombinationFailureReason> failures =
                new List<ChipCombinationFailureReason>();
            int actionCount = actions != null ? actions.Count : 0;
            if (actionCount < 1 || actionCount > ChipCombinationSelectionRules.MaxActionCount(profession))
            {
                Add(failures, "ActionCount", "BDP_ChipManufacturing_InvalidActionCount");
                return failures;
            }

            HashSet<string> actionNames = new HashSet<string>();
            for (int index = 0; index < actionCount; index++)
            {
                ChipActionPresetDef action = actions[index];
                if (action == null)
                {
                    Add(failures, "MissingAction", "BDP_ChipManufacturing_MissingAction");
                    continue;
                }

                if (!actionNames.Add(action.defName))
                {
                    Add(failures, "DuplicateAction", "BDP_ChipManufacturing_DuplicateAction");
                }

                if (action.config?.Profile?.Category != category)
                {
                    Add(failures, "CategoryMismatch", "BDP_ChipManufacturing_CategoryMismatch");
                }

                if (!ChipCombinationSelectionRules.CanUseAction(profession, action))
                {
                    Add(failures, "ProfessionMismatch", "BDP_ChipManufacturing_ProfessionMismatch");
                }

                if (actionCount > 1
                    && ChipCombinationSelectionRules.HasIntrinsicMultipleModes(action))
                {
                    Add(
                        failures,
                        "IntrinsicMultiMode",
                        "BDP_ChipManufacturing_IntrinsicMultiModeMustStandAlone");
                }
            }

            if (gunShell != null
                && (profession == null
                    || gunShell.compatibleProfessions == null
                    || !gunShell.compatibleProfessions.Contains(profession)))
            {
                Add(failures, "GunShellProfession", "BDP_ChipManufacturing_GunShellProfessionMismatch");
            }

            if (actionCount == 2)
            {
                ValidateDualStructure(actions[0], actions[1], failures);
            }

            return failures;
        }

        /// <summary>检查双动作必须相同的结构，以及显式可合并字段。</summary>
        private static void ValidateDualStructure(
            ChipActionPresetDef first,
            ChipActionPresetDef second,
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
