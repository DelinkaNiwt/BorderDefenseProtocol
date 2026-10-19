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
                    null,
                    new ChipCombinationFailureReason(
                        "NullRecord",
                        "BDP_ChipManufacturing_NullRecord"));
            }

            ChipCombinationFailureReason malformedRecord =
                FindMalformedRecord(record);
            if (malformedRecord != null)
            {
                return Invalid(record.ProfessionDefName, null, null, malformedRecord);
            }

            ChipCategoryDef category =
                ChipManufacturingDefLookup.FindCategory(record.CategoryDefName);
            ChipProfessionDef profession =
                ChipManufacturingDefLookup.FindProfession(record.ProfessionDefName);
            List<ChipManufacturingPresetDef> functionDefs = ResolveChipFunctionDefs(record);
            ChipArmamentFormDef armamentForm =
                ChipManufacturingDefLookup.FindArmamentForm(record.ArmamentFormDefName)
                ?? ChipManufacturingDefLookup.FindImplicitDefaultArmamentForm(
                    category,
                    profession,
                    functionDefs);

            List<ChipCombinationFailureReason> missing = FindMissingSources(
                record,
                category,
                profession,
                functionDefs,
                armamentForm);
            if (missing.Count > 0)
            {
                return new ChipCombinationResolution
                {
                    Status = ChipCombinationResolutionStatus.Invalid,
                    ResolvedLabel = record.LastResolvedLabel,
                    SourceProfessionKey = record.ProfessionDefName,
                    FunctionDefs = functionDefs,
                    ArmamentForm = armamentForm,
                    FailureReasons = missing
                };
            }

            List<ChipCombinationFailureReason> failures =
                ChipCombinationCompatibilityService.Validate(
                    category,
                    profession,
                    functionDefs,
                    armamentForm);
            if (failures.Count > 0)
            {
                return new ChipCombinationResolution
                {
                    Status = ChipCombinationResolutionStatus.Invalid,
                    ResolvedLabel = record.LastResolvedLabel,
                    SourceProfessionKey = record.ProfessionDefName,
                    FunctionDefs = functionDefs,
                    ArmamentForm = armamentForm,
                    FailureReasons = failures
                };
            }

            ChipDefinitionConfig config = functionDefs.Count == 1
                ? ChipConfigurationMergeService.CloneSingle(functionDefs[0])
                : ChipConfigurationMergeService.MergeDual(functionDefs[0], functionDefs[1]);
            ChipDefinitionConfig declaredConfig = ChipConfigurationMergeService.CloneConfig(config);
            ChipArmamentFormApplicationService.Apply(config, armamentForm);
            List<string> functionLabels = new List<string>();
            for (int index = 0; index < functionDefs.Count; index++)
            {
                functionLabels.Add(functionDefs[index].ResolvedLabel);
            }

            string functionLabel = string.Join("/", functionLabels);
            string label = armamentForm == null || !armamentForm.includeInProductLabel
                ? "BDP_ChipManufacturing_ProductLabel".Translate(functionLabel)
                : "BDP_ChipManufacturing_ProductLabelWithArmamentForm".Translate(
                    functionLabel,
                    armamentForm.ResolvedLabel);
            record.LastResolvedLabel = label;

            return new ChipCombinationResolution
            {
                Status = ChipCombinationResolutionStatus.Valid,
                ResolvedLabel = label,
                SourceProfessionKey = record.ProfessionDefName,
                ResolvedConfig = config,
                DeclaredConfig = declaredConfig,
                FunctionDefs = functionDefs,
                ArmamentForm = armamentForm,
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

            if (record.OrderedChipFunctionDefNames == null
                || record.OrderedChipFunctionDefNames.Count == 0)
            {
                return new ChipCombinationFailureReason(
                    "EmptyFunctionList",
                    "BDP_ChipManufacturing_EmptyFunctionList");
            }

            for (int index = 0; index < record.OrderedChipFunctionDefNames.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(record.OrderedChipFunctionDefNames[index]))
                {
                    return new ChipCombinationFailureReason(
                        "EmptyFunctionKey",
                        "BDP_ChipManufacturing_EmptyFunctionKey");
                }
            }

            return null;
        }

        /// <summary>按记录顺序查找芯片功能定义，并以 null 占住缺失来源的位置。</summary>
        private static List<ChipManufacturingPresetDef> ResolveChipFunctionDefs(ChipCombinationRecord record)
        {
            List<ChipManufacturingPresetDef> functionDefs = new List<ChipManufacturingPresetDef>();
            if (record.OrderedChipFunctionDefNames == null)
            {
                return functionDefs;
            }

            for (int index = 0; index < record.OrderedChipFunctionDefNames.Count; index++)
            {
                functionDefs.Add(ChipManufacturingDefLookup.FindChipFunctionDef(
                    record.OrderedChipFunctionDefNames[index]));
            }

            return functionDefs;
        }

        /// <summary>在业务合法性之前，完整识别所有命名来源缺失。</summary>
        private static List<ChipCombinationFailureReason> FindMissingSources(
            ChipCombinationRecord record,
            ChipCategoryDef category,
            ChipProfessionDef profession,
            IList<ChipManufacturingPresetDef> functionDefs,
            ChipArmamentFormDef armamentForm)
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

            for (int index = 0; index < functionDefs.Count; index++)
            {
                if (functionDefs[index] == null)
                {
                    failures.Add(Missing("Function"));
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(record.ArmamentFormDefName) && armamentForm == null)
            {
                failures.Add(Missing("ArmamentForm"));
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
            string sourceProfessionKey,
            IReadOnlyList<ChipManufacturingPresetDef> functionDefs,
            ChipArmamentFormDef armamentForm,
            ChipCombinationFailureReason failure)
        {
            return new ChipCombinationResolution
            {
                Status = ChipCombinationResolutionStatus.Invalid,
                SourceProfessionKey = sourceProfessionKey,
                FunctionDefs = functionDefs,
                ArmamentForm = armamentForm,
                FailureReasons = new[] { failure }
            };
        }
    }
}
