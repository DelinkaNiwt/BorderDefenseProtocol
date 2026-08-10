using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Core.Chips;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>从持久化选择记录重建当前芯片结果的单一业务入口。</summary>
    public sealed class ChipCombinationResolver
    {
        /// <summary>解析组合；只在成功时更新记录的最后成功名称。</summary>
        public ChipCombinationResolution Resolve(ChipCombinationRecord record)
        {
            if (record == null)
            {
                return Invalid(
                    null,
                    null,
                    new ChipCombinationFailureReason(
                        "NullRecord",
                        "BDP_ChipManufacturing_NullRecord"));
            }

            ChipCombinationFailureReason malformedRecord =
                FindMalformedRecord(record);
            if (malformedRecord != null)
            {
                return Invalid(null, null, malformedRecord);
            }

            ChipCategoryDef category =
                ChipManufacturingDefLookup.FindCategory(record.CategoryDefName);
            ChipProfessionDef profession =
                ChipManufacturingDefLookup.FindProfession(record.ProfessionDefName);
            ChipGunShellDef gunShell =
                ChipManufacturingDefLookup.FindGunShell(record.GunShellDefName);
            List<ChipActionPresetDef> actions = ResolveActions(record);

            List<ChipCombinationFailureReason> missing = FindMissingSources(
                record,
                category,
                profession,
                actions,
                gunShell);
            if (missing.Count > 0)
            {
                return new ChipCombinationResolution
                {
                    Status = ChipCombinationResolutionStatus.MissingSource,
                    ResolvedLabel = record.LastResolvedLabel,
                    Actions = actions,
                    GunShell = gunShell,
                    FailureReasons = missing
                };
            }

            List<ChipCombinationFailureReason> failures =
                ChipCombinationCompatibilityService.Validate(
                    category,
                    profession,
                    actions,
                    gunShell);
            if (failures.Count > 0)
            {
                return new ChipCombinationResolution
                {
                    Status = ChipCombinationResolutionStatus.Invalid,
                    ResolvedLabel = record.LastResolvedLabel,
                    Actions = actions,
                    GunShell = gunShell,
                    FailureReasons = failures
                };
            }

            ChipDefinitionConfig config = actions.Count == 1
                ? ChipConfigurationMergeService.CloneSingle(actions[0])
                : ChipConfigurationMergeService.MergeDual(actions[0], actions[1]);
            ChipGunShellApplicationService.Apply(config, gunShell);
            List<string> actionLabels = new List<string>();
            for (int index = 0; index < actions.Count; index++)
            {
                actionLabels.Add(actions[index].label);
            }

            string actionLabel = string.Join("/", actionLabels);
            string label = gunShell == null
                ? "BDP_ChipManufacturing_ProductLabel".Translate(actionLabel)
                : "BDP_ChipManufacturing_ProductLabelWithGunShell".Translate(
                    actionLabel,
                    gunShell.label);
            record.LastResolvedLabel = label;

            return new ChipCombinationResolution
            {
                Status = ChipCombinationResolutionStatus.Valid,
                ResolvedLabel = label,
                ResolvedConfig = config,
                Actions = actions,
                GunShell = gunShell,
                FailureReasons = new List<ChipCombinationFailureReason>()
            };
        }

        /// <summary>区分记录自身缺项与引用了暂时不存在的外部来源。</summary>
        private static ChipCombinationFailureReason FindMalformedRecord(
            ChipCombinationRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.CategoryDefName))
            {
                return new ChipCombinationFailureReason(
                    "EmptyCategory",
                    "BDP_ChipManufacturing_EmptyCategory");
            }

            if (record.OrderedActionPresetDefNames == null
                || record.OrderedActionPresetDefNames.Count == 0)
            {
                return new ChipCombinationFailureReason(
                    "EmptyActionList",
                    "BDP_ChipManufacturing_EmptyActionList");
            }

            for (int index = 0; index < record.OrderedActionPresetDefNames.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(record.OrderedActionPresetDefNames[index]))
                {
                    return new ChipCombinationFailureReason(
                        "EmptyActionKey",
                        "BDP_ChipManufacturing_EmptyActionKey");
                }
            }

            return null;
        }

        /// <summary>按记录顺序查找动作，并以 null 占住缺失来源的位置。</summary>
        private static List<ChipActionPresetDef> ResolveActions(ChipCombinationRecord record)
        {
            List<ChipActionPresetDef> actions = new List<ChipActionPresetDef>();
            if (record.OrderedActionPresetDefNames == null)
            {
                return actions;
            }

            for (int index = 0; index < record.OrderedActionPresetDefNames.Count; index++)
            {
                actions.Add(ChipManufacturingDefLookup.FindAction(
                    record.OrderedActionPresetDefNames[index]));
            }

            return actions;
        }

        /// <summary>在业务合法性之前，完整识别所有命名来源缺失。</summary>
        private static List<ChipCombinationFailureReason> FindMissingSources(
            ChipCombinationRecord record,
            ChipCategoryDef category,
            ChipProfessionDef profession,
            IList<ChipActionPresetDef> actions,
            ChipGunShellDef gunShell)
        {
            List<ChipCombinationFailureReason> failures =
                new List<ChipCombinationFailureReason>();
            if (category == null)
            {
                failures.Add(Missing("Category"));
            }

            if (!string.IsNullOrWhiteSpace(record.ProfessionDefName) && profession == null)
            {
                failures.Add(Missing("Profession"));
            }

            for (int index = 0; index < actions.Count; index++)
            {
                if (actions[index] == null)
                {
                    failures.Add(Missing("Action"));
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(record.GunShellDefName) && gunShell == null)
            {
                failures.Add(Missing("GunShell"));
            }

            return failures;
        }

        /// <summary>构造一条稳定来源缺失原因。</summary>
        private static ChipCombinationFailureReason Missing(string sourceKind)
        {
            return new ChipCombinationFailureReason(
                "Missing" + sourceKind,
                "BDP_ChipManufacturing_Missing" + sourceKind);
        }

        /// <summary>构造无来源上下文的非法结果。</summary>
        private static ChipCombinationResolution Invalid(
            IReadOnlyList<ChipActionPresetDef> actions,
            ChipGunShellDef gunShell,
            ChipCombinationFailureReason failure)
        {
            return new ChipCombinationResolution
            {
                Status = ChipCombinationResolutionStatus.Invalid,
                Actions = actions,
                GunShell = gunShell,
                FailureReasons = new[] { failure }
            };
        }
    }
}
