using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 按固定值层级裁定一个功能属性的多来源贡献。
    /// </summary>
    public static class ChipFeatureContributionMergeService
    {
        /// <summary>比较普通数值时允许的浮点误差。</summary>
        private const float NumberEpsilon = 0.0001f;

        /// <summary>
        /// 合并同一属性的贡献。
        /// 运行时值优先于有效定义值，后者优先于声明值；同层冲突不猜测。
        /// </summary>
        public static ChipInformationProperty Merge(
            ChipFeaturePropertyDef property,
            IEnumerable<ChipFeatureValueContribution> contributions,
            string featurePath,
            ICollection<ChipInformationDiagnostic> diagnostics)
        {
            List<ChipFeatureValueContribution> all =
                new List<ChipFeatureValueContribution>();
            if (contributions != null)
            {
                foreach (ChipFeatureValueContribution contribution in contributions)
                {
                    if (contribution != null)
                    {
                        all.Add(contribution);
                    }
                }
            }

            List<ChipInformationProvenance> provenance =
                CollectProvenance(all);
            if (property == null)
            {
                return null;
            }

            if (all.Count == 0)
            {
                return null;
            }

            ChipFeatureValueLayer highestLayer = HighestLayer(all);
            List<ChipFeatureValueContribution> selected =
                all.FindAll(item => item.Layer == highestLayer);
            for (int index = 0; index < selected.Count; index++)
            {
                ChipFeatureValueContribution item = selected[index];
                if (item.Value == null || item.Value.Kind != property.ValueKind)
                {
                    diagnostics?.Add(new ChipInformationDiagnostic(
                        "feature.property_value_type_mismatch",
                        featurePath,
                        property.defName,
                        ChipInformationDiagnosticSeverity.Error,
                        null));
                    return null;
                }
            }

            ChipInformationValue firstValue = selected[0].Value;
            for (int index = 1; index < selected.Count; index++)
            {
                if (!AreEqual(firstValue, selected[index].Value))
                {
                    diagnostics?.Add(new ChipInformationDiagnostic(
                        "feature.property_value_conflict",
                        featurePath,
                        property.defName,
                        ChipInformationDiagnosticSeverity.Error,
                        null));
                    return null;
                }
            }

            return CreateProperty(property, firstValue, provenance);
        }

        /// <summary>把 Def 与解析值写入统一属性结构。</summary>
        private static ChipInformationProperty CreateProperty(
            ChipFeaturePropertyDef property,
            ChipInformationValue value,
            IEnumerable<ChipInformationProvenance> sources)
        {
            return new ChipInformationProperty(
                property?.defName,
                null,
                property?.ValueKind ?? ChipInformationValueKind.Text,
                value,
                property?.UnitKey,
                sources);
        }

        /// <summary>取得贡献集合的最高值层级。</summary>
        private static ChipFeatureValueLayer HighestLayer(
            IList<ChipFeatureValueContribution> contributions)
        {
            bool hasRuntime = false;
            bool hasEffectiveDefinition = false;
            for (int index = 0; index < contributions.Count; index++)
            {
                if (contributions[index].Layer == ChipFeatureValueLayer.Runtime)
                {
                    hasRuntime = true;
                }
                else if (contributions[index].Layer
                    == ChipFeatureValueLayer.EffectiveDefinition)
                {
                    hasEffectiveDefinition = true;
                }
            }

            if (hasRuntime)
            {
                return ChipFeatureValueLayer.Runtime;
            }

            if (hasEffectiveDefinition)
            {
                return ChipFeatureValueLayer.EffectiveDefinition;
            }

            return ChipFeatureValueLayer.DeclaredDefinition;
        }

        /// <summary>合并来源链且保留来源顺序。</summary>
        private static List<ChipInformationProvenance> CollectProvenance(
            IEnumerable<ChipFeatureValueContribution> contributions)
        {
            List<ChipInformationProvenance> result =
                new List<ChipInformationProvenance>();
            foreach (ChipFeatureValueContribution contribution in contributions)
            {
                if (contribution.Provenance == null)
                {
                    continue;
                }

                for (int index = 0; index < contribution.Provenance.Count; index++)
                {
                    ChipInformationProvenance item = contribution.Provenance[index];
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        /// <summary>比较两个类型化值是否语义相等。</summary>
        private static bool AreEqual(
            ChipInformationValue first,
            ChipInformationValue second)
        {
            if (first == null || second == null || first.Kind != second.Kind)
            {
                return first == second;
            }

            switch (first.Kind)
            {
                case ChipInformationValueKind.Text:
                    return string.Equals(first.Text, second.Text, StringComparison.Ordinal);
                case ChipInformationValueKind.Boolean:
                    return first.Boolean == second.Boolean;
                case ChipInformationValueKind.Integer:
                    return first.Integer == second.Integer;
                case ChipInformationValueKind.Number:
                case ChipInformationValueKind.Percentage:
                case ChipInformationValueKind.Duration:
                    return Math.Abs(first.Number - second.Number) <= NumberEpsilon;
                case ChipInformationValueKind.Enum:
                    return first.DisplaySourceKind == second.DisplaySourceKind
                        && string.Equals(first.EnumKey, second.EnumKey, StringComparison.Ordinal)
                        && string.Equals(first.DisplayKey, second.DisplayKey, StringComparison.Ordinal)
                        && string.Equals(first.DefinitionTypeKey, second.DefinitionTypeKey, StringComparison.Ordinal)
                        && string.Equals(first.DefinitionName, second.DefinitionName, StringComparison.Ordinal);
                case ChipInformationValueKind.List:
                    if (first.Items.Count != second.Items.Count)
                    {
                        return false;
                    }

                    for (int index = 0; index < first.Items.Count; index++)
                    {
                        if (!AreEqual(first.Items[index], second.Items[index]))
                        {
                            return false;
                        }
                    }

                    return true;
                case ChipInformationValueKind.Record:
                    if (first.Fields.Count != second.Fields.Count)
                    {
                        return false;
                    }

                    for (int index = 0; index < first.Fields.Count; index++)
                    {
                        ChipInformationValueField firstField = first.Fields[index];
                        ChipInformationValueField secondField = second.Fields[index];
                        if (firstField == null || secondField == null
                            || !string.Equals(firstField.Key, secondField.Key, StringComparison.Ordinal)
                            || !string.Equals(firstField.UnitKey, secondField.UnitKey, StringComparison.Ordinal)
                            || !AreEqual(firstField.Value, secondField.Value))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    return false;
            }
        }
    }
}
