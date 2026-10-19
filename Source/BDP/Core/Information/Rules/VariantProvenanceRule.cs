using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>公开通用来源索引，保留基础值与构型值的可追踪入口。</summary>
    internal sealed class VariantProvenanceRule : IChipInformationRule
    {
        /// <summary>规则键。</summary>
        public string RuleKey => "provenance-sources";

        /// <summary>判断是否有来源链记录。</summary>
        public bool CanApply(ChipInformationRuleContext context)
        {
            return context != null
                && context.Input != null
                && context.Input.ProvenanceSources != null
                && context.Input.ProvenanceSources.Count > 0;
        }

        /// <summary>声明来源索引字段。</summary>
        public IReadOnlyList<ChipInformationBlockSchema> DescribeSchema(
            ChipInformationRuleContext context)
        {
            List<ChipInformationFieldSchema> fields = new List<ChipInformationFieldSchema>();
            if (!CanApply(context))
            {
                return new List<ChipInformationBlockSchema>();
            }

            for (int index = 0; index < context.Input.ProvenanceSources.Count; index++)
            {
                ChipInformationProvenance source = context.Input.ProvenanceSources[index];
                if (source == null || string.IsNullOrWhiteSpace(source.SourceKey))
                {
                    continue;
                }

                fields.Add(new ChipInformationFieldSchema(
                    "source." + source.SourceKey + ".kind",
                    "BDP_Info_Field_SourceKind",
                    ChipInformationValueKind.Enum,
                    null,
                    false));
            }

            return new[]
            {
                new ChipInformationBlockSchema(
                    "provenance",
                    "provenance",
                    "BDP_Info_Block_Provenance",
                    900,
                    fields)
            };
        }

        /// <summary>解析来源索引字段。</summary>
        public IReadOnlyList<ChipInformationContribution> Resolve(
            ChipInformationRuleContext context)
        {
            List<ChipInformationContribution> result =
                new List<ChipInformationContribution>();
            if (!CanApply(context))
            {
                return result;
            }

            for (int index = 0; index < context.Input.ProvenanceSources.Count; index++)
            {
                ChipInformationProvenance source = context.Input.ProvenanceSources[index];
                if (source == null || string.IsNullOrWhiteSpace(source.SourceKey))
                {
                    continue;
                }

                string fieldKey = "source." + source.SourceKey + ".kind";
                ChipInformationValue value = ChipInformationValue.FromLocalizedEnum(
                    source.SourceKind.ToString(),
                    GetSourceKindTranslationKey(source.SourceKind));
                ChipInformationProperty property = new ChipInformationProperty(
                    fieldKey,
                    "BDP_Info_Field_SourceKind",
                    ChipInformationValueKind.Enum,
                    value,
                    null,
                    new[] { source });
                result.Add(new ChipInformationContribution(
                    "provenance",
                    "provenance",
                    "BDP_Info_Block_Provenance",
                    900,
                    new ChipInformationFieldSchema(
                        fieldKey,
                        "BDP_Info_Field_SourceKind",
                        ChipInformationValueKind.Enum,
                        null,
                        false),
                    property));
            }

            return result;
        }

        /// <summary>映射已有来源类别到制造信息报告共用的本地化类别名。</summary>
        private static string GetSourceKindTranslationKey(ChipInformationSourceKind kind)
        {
            switch (kind)
            {
                case ChipInformationSourceKind.BaseDefinition:
                case ChipInformationSourceKind.DeclaredDefinition:
                case ChipInformationSourceKind.EffectiveDefinition:
                    return "BDP_Info_Report_Source_BaseDefinition";
                case ChipInformationSourceKind.Variant:
                    return "BDP_Info_Report_Source_Variant";
                case ChipInformationSourceKind.RuntimeResult:
                    return "BDP_Info_Report_Source_Runtime";
                default:
                    return "BDP_Info_Report_Source_Other";
            }
        }
    }
}
