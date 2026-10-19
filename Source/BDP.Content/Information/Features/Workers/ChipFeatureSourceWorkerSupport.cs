using System.Collections.Generic;
using System;
using BDP.Core.AttackExecution;
using BDP.Core.CombatModel;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Core.Information.Vanilla;
using BDP.Content.Information.Features.Sources;
using RimWorld;
using Verse;

namespace BDP.Content.Information.Features.Workers
{
    /// <summary>
    /// BDP 标准来源 Worker 共用的只读表达条目查找和属性贡献辅助方法。
    /// </summary>
    internal static class ChipFeatureSourceWorkerSupport
    {
        /// <summary>按有效配置优先、并遵守形态姿态活动条目定位单条来源。</summary>
        public static ChipExpressionEntryConfig ResolveEntry(
            ExpressionEntryFeatureSourceConfig source,
            ChipFeatureReadContext context)
        {
            if (source == null || context == null)
            {
                return null;
            }

            ChipExpressionConfig expression = context.EffectiveConfig?.Expression
                ?? context.DeclaredConfig?.Expression;
            if (expression?.Entries == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(source.EntryId)
                && IsEntryActive(context, source.EntryId))
            {
                ChipExpressionEntryConfig entry =
                    FindEntry(expression.Entries, source.EntryId);
                if (entry != null)
                {
                    return entry;
                }
            }

            if (source.EntryIds == null)
            {
                return null;
            }

            for (int index = 0; index < source.EntryIds.Count; index++)
            {
                string entryId = source.EntryIds[index];
                if (string.IsNullOrWhiteSpace(entryId)
                    || !IsEntryActive(context, entryId))
                {
                    continue;
                }

                ChipExpressionEntryConfig entry =
                    FindEntry(expression.Entries, entryId);
                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>读取 Verb 来源或 AbilityDef 上的有效原版攻击参数。</summary>
        public static VerbProperties ResolveVerbProperties(
            ChipExpressionEntryConfig entry)
        {
            if (entry?.VerbProps != null)
            {
                return entry.VerbProps;
            }

            if (entry?.EntryType != ChipExpressionEntryTypeConfig.Ability
                || string.IsNullOrWhiteSpace(entry.AbilityDefName))
            {
                return null;
            }

            return DefDatabase<AbilityDef>.GetNamedSilentFail(entry.AbilityDefName)
                ?.verbProperties;
        }

        /// <summary>构建近战表达条目的原版 Tool 表面，不修改声明对象。</summary>
        public static IReadOnlyList<MeleeToolSurface> BuildMeleeToolSurfaces(
            ChipExpressionEntryConfig entry)
        {
            List<Tool> tools = new List<Tool>();
            if (entry?.Tool != null)
            {
                tools.Add(entry.Tool);
            }

            if (entry?.tools != null)
            {
                for (int index = 0; index < entry.tools.Count; index++)
                {
                    Tool tool = entry.tools[index];
                    if (tool != null && !ReferenceEquals(tool, entry.Tool))
                    {
                        tools.Add(tool);
                    }
                }
            }

            AttackExecutionStyle style = null;
            if (entry?.Execution != null)
            {
                style = new AttackExecutionStyle
                {
                    Single = new SingleAttackExecutionStyle
                    {
                        meleeHitCount = entry.Execution.HitCount > 0
                            ? entry.Execution.HitCount
                            : 1,
                        meleeHitIntervalTicks = entry.Execution.HitIntervalTicks > 0
                            ? entry.Execution.HitIntervalTicks
                            : 0
                    }
                };
            }

            return MeleeToolSurfaceBuilder.Build(entry, tools, style);
        }

        /// <summary>添加可显式保留零值的语义属性贡献。</summary>
        public static void AddSemanticValue(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            string propertyDefName,
            ChipInformationValue value,
            ChipInformationSourceKind sourceKind)
        {
            if (target == null || source == null || string.IsNullOrWhiteSpace(propertyDefName)
                || value == null)
            {
                return;
            }

            ChipFeaturePropertyDef property = FindProperty(propertyDefName);
            if (property == null)
            {
                return;
            }

            target.Add(new ChipFeatureValueContribution(
                property,
                value,
                ChipFeatureValueLayer.EffectiveDefinition,
                new[] { Provenance(source, value, sourceKind) }));
        }

        /// <summary>添加普通数值；不适用值须由调用方在调用前判定。</summary>
        public static void AddSemanticNumber(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            string propertyDefName,
            float value,
            ChipInformationSourceKind sourceKind)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return;
            }

            AddSemanticValue(
                target,
                source,
                propertyDefName,
                ChipInformationValue.FromNumber(value),
                sourceKind);
        }

        /// <summary>添加秒制持续时间，允许精确的零秒值。</summary>
        public static void AddSemanticDuration(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            string propertyDefName,
            float value,
            ChipInformationSourceKind sourceKind)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return;
            }

            AddSemanticValue(
                target,
                source,
                propertyDefName,
                ChipInformationValue.FromDuration(value),
                sourceKind);
        }

        /// <summary>添加比例值，零比例以有效值保留。</summary>
        public static void AddSemanticPercentage(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            string propertyDefName,
            float value,
            ChipInformationSourceKind sourceKind)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return;
            }

            AddSemanticValue(
                target,
                source,
                propertyDefName,
                ChipInformationValue.FromPercentage(value),
                sourceKind);
        }

        /// <summary>添加整数值，零数量以有效值保留。</summary>
        public static void AddSemanticInteger(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            string propertyDefName,
            int value,
            ChipInformationSourceKind sourceKind)
        {
            AddSemanticValue(
                target,
                source,
                propertyDefName,
                ChipInformationValue.FromInteger(value),
                sourceKind);
        }

        /// <summary>添加布尔值，包括明确声明的 false。</summary>
        public static void AddSemanticBoolean(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            string propertyDefName,
            bool value,
            ChipInformationSourceKind sourceKind)
        {
            AddSemanticValue(
                target,
                source,
                propertyDefName,
                ChipInformationValue.FromBoolean(value),
                sourceKind);
        }

        /// <summary>添加带稳定枚举键的只读列表值。</summary>
        public static void AddSemanticList(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            string propertyDefName,
            IReadOnlyList<string> values,
            ChipInformationSourceKind sourceKind)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            List<ChipInformationValue> items = new List<ChipInformationValue>();
            for (int index = 0; index < values.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(values[index]))
                {
                    items.Add(ChipInformationValue.FromTranslationKey(values[index]));
                }
            }

            if (items.Count > 0)
            {
                AddSemanticValue(
                    target,
                    source,
                    propertyDefName,
                    ChipInformationValue.FromList(items),
                    sourceKind);
            }
        }

        /// <summary>按原始顺序添加来源数量与每秒消耗的完整分档表。</summary>
        public static void AddSemanticSustainCostBySourceCount(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            ChipExpressionEntryConfig entry,
            ChipInformationSourceKind sourceKind)
        {
            if (entry?.Trion?.SustainCostBySourceCount == null
                || entry.Trion.SustainCostBySourceCount.Count == 0)
            {
                return;
            }

            List<ChipInformationValue> rows = new List<ChipInformationValue>();
            for (int index = 0; index < entry.Trion.SustainCostBySourceCount.Count; index++)
            {
                ExpressionSustainCostBySourceCountConfig row =
                    entry.Trion.SustainCostBySourceCount[index];
                if (row == null)
                {
                    continue;
                }

                rows.Add(ChipInformationValue.FromRecord(new[]
                {
                    new ChipInformationValueField(
                        "sourceCount",
                        ChipInformationValue.FromInteger(row.SourceCount),
                        "BDP_Info_Field_SourceCount"),
                    new ChipInformationValueField(
                        "totalPerSecond",
                        ChipInformationValue.FromNumber(row.TotalPerSecond),
                        "BDP_Info_Field_TotalPerSecond",
                        "BDP_Info_Unit_TrionPerSecond")
                }));
            }

            if (rows.Count > 0)
            {
                AddSemanticValue(
                    target,
                    source,
                    "BDP_Property_SustainCost",
                    ChipInformationValue.FromList(rows),
                    sourceKind);
            }
        }

        /// <summary>把原版目标参数映射为稳定目标类别，不展示原版类名。</summary>
        public static void AddTargetTypes(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            TargetingParameters targetParams,
            ChipInformationSourceKind sourceKind)
        {
            if (targetParams == null)
            {
                return;
            }

            List<string> values = new List<string>();
            if (targetParams.canTargetSelf)
            {
                values.Add("BDP_TargetType_Self");
            }

            if (targetParams.canTargetPawns
                || targetParams.canTargetAnimals
                || targetParams.canTargetHumans
                || targetParams.canTargetMechs
                || targetParams.canTargetEntities)
            {
                values.Add("BDP_TargetType_Pawn");
            }

            if (targetParams.canTargetBuildings)
            {
                values.Add("BDP_TargetType_Building");
            }

            if (targetParams.canTargetLocations)
            {
                values.Add("BDP_TargetType_Location");
            }

            if (targetParams.canTargetItems
                || targetParams.canTargetFires
                || targetParams.canTargetPlants
                || targetParams.canTargetCorpses)
            {
                values.Add("BDP_TargetType_Thing");
            }

            AddSemanticList(
                target,
                source,
                "BDP_Property_TargetType",
                values,
                sourceKind);
        }

        /// <summary>有上下文时只读取当前形态/姿态激活的条目。</summary>
        private static bool IsEntryActive(
            ChipFeatureReadContext context,
            string entryId)
        {
            if (context?.ActiveEntryIds == null)
            {
                return true;
            }

            for (int index = 0; index < context.ActiveEntryIds.Count; index++)
            {
                if (string.Equals(context.ActiveEntryIds[index], entryId, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>按稳定 Id 查找表达条目。</summary>
        private static ChipExpressionEntryConfig FindEntry(
            IList<ChipExpressionEntryConfig> entries,
            string entryId)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                ChipExpressionEntryConfig entry = entries[index];
                if (entry != null && string.Equals(entry.Id, entryId, System.StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>按属性 DefName 创建来源贡献。</summary>
        private static ChipFeaturePropertyDef FindProperty(string defName)
        {
            return DefDatabase<ChipFeaturePropertyDef>.GetNamedSilentFail(defName);
        }

        /// <summary>创建一个基础来源记录。</summary>
        private static ChipInformationProvenance Provenance(
            ChipFeatureSourceConfig source,
            ChipInformationValue value,
            ChipInformationSourceKind sourceKind)
        {
            return new ChipInformationProvenance(
                source.Id,
                sourceKind,
                ChipInformationContributionKind.Base,
                value,
                null,
                true);
        }

    }
}
