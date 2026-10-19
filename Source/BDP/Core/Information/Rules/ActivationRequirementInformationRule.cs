using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>公开芯片定义中声明的原始激活条件数据。</summary>
    internal sealed class ActivationRequirementInformationRule : IChipInformationRule
    {
        /// <summary>规则键。</summary>
        public string RuleKey => "activation-requirements";

        /// <summary>判断输入是否存在激活条件声明。</summary>
        public bool CanApply(ChipInformationRuleContext context)
        {
            return context?.Input?.Facts?.ActivationConditions != null
                && context.Input.Facts.ActivationConditions.Count > 0;
        }

        /// <summary>声明已存在的原始条件属性。</summary>
        public IReadOnlyList<ChipInformationBlockSchema> DescribeSchema(
            ChipInformationRuleContext context)
        {
            List<ChipInformationFieldSchema> fields = new List<ChipInformationFieldSchema>();
            if (!CanApply(context))
            {
                return new List<ChipInformationBlockSchema>();
            }

            for (int index = 0; index < context.Input.Facts.ActivationConditions.Count; index++)
            {
                ChipInformationConditionFact condition =
                    context.Input.Facts.ActivationConditions[index];
                AddSchema(fields, condition, "type",
                    string.IsNullOrWhiteSpace(condition?.ConditionTypeKey)
                        ? (ChipInformationValueKind?)null
                        : ChipInformationValueKind.Enum,
                    null);
                AddSchema(fields, condition, "required", condition?.RequiredValue?.Kind,
                    condition?.RequiredUnitKey);
                AddSchema(fields, condition, "reference", condition?.ReferenceValue?.Kind, null);
            }

            return new[]
            {
                new ChipInformationBlockSchema(
                    "activation",
                    "activation",
                    "BDP_Info_Block_Activation",
                    40,
                    fields)
            };
        }

        /// <summary>解析原始条件类型、门槛和定义引用。</summary>
        public IReadOnlyList<ChipInformationContribution> Resolve(
            ChipInformationRuleContext context)
        {
            List<ChipInformationContribution> result =
                new List<ChipInformationContribution>();
            if (!CanApply(context))
            {
                return result;
            }

            for (int index = 0; index < context.Input.Facts.ActivationConditions.Count; index++)
            {
                ChipInformationConditionFact condition =
                    context.Input.Facts.ActivationConditions[index];
                if (condition == null || string.IsNullOrWhiteSpace(condition.ConditionKey))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(condition.ConditionTypeKey))
                {
                    AddContribution(result, condition, "type",
                        ChipInformationValue.FromEnum(condition.ConditionTypeKey), null);
                }
                AddContribution(result, condition, "required",
                    condition.RequiredValue, condition.RequiredUnitKey);
                AddContribution(result, condition, "reference",
                    condition.ReferenceValue, null);
            }

            return result;
        }

        /// <summary>为存在的原始值声明字段模式。</summary>
        private static void AddSchema(
            ICollection<ChipInformationFieldSchema> target,
            ChipInformationConditionFact condition,
            string suffix,
            ChipInformationValueKind? valueKind,
            string unitKey)
        {
            if (target == null || condition == null
                || string.IsNullOrWhiteSpace(condition.ConditionKey)
                || !valueKind.HasValue)
            {
                return;
            }

            target.Add(new ChipInformationFieldSchema(
                FieldKey(condition, suffix),
                condition.LabelKey,
                valueKind.Value,
                unitKey,
                true));
        }

        /// <summary>为存在的原始值创建基础来源字段。</summary>
        private static void AddContribution(
            ICollection<ChipInformationContribution> target,
            ChipInformationConditionFact condition,
            string suffix,
            ChipInformationValue value,
            string unitKey)
        {
            if (target == null || condition == null || value == null
                || string.IsNullOrWhiteSpace(condition.ConditionKey))
            {
                return;
            }

            string fieldKey = FieldKey(condition, suffix);
            ChipInformationFieldSchema schema = new ChipInformationFieldSchema(
                fieldKey,
                condition.LabelKey,
                value.Kind,
                unitKey,
                true);
            ChipInformationProperty property = new ChipInformationProperty(
                fieldKey,
                condition.LabelKey,
                value.Kind,
                value,
                unitKey,
                new[]
                {
                    new ChipInformationProvenance(
                        "definition.activation." + condition.ConditionKey,
                        ChipInformationSourceKind.ActivationRequirement,
                        ChipInformationContributionKind.Base,
                        value,
                        null,
                        true)
                });
            target.Add(new ChipInformationContribution(
                "activation",
                "activation",
                "BDP_Info_Block_Activation",
                40,
                schema,
                property));
        }

        /// <summary>创建条件属性的稳定字段键。</summary>
        private static string FieldKey(
            ChipInformationConditionFact condition,
            string suffix)
        {
            return "condition." + condition.ConditionKey + "." + suffix;
        }
    }
}
