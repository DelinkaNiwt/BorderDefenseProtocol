using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using BDP.Core.Expressions.Runtime;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技定义最低合法性校验器。
    /// 它负责把来源芯片依赖问题尽量拦在读取期，而不是拖到攻击期。
    /// </summary>
    internal sealed class ComboDefinitionValidator
    {
        /// <summary>
        /// 校验指定组合技契约是否满足最低正式要求。
        /// </summary>
        public ComboDefinitionValidationResult Validate(ComboDefinitionContract contract)
        {
            List<ComboDefinitionValidationMessage> errors = new List<ComboDefinitionValidationMessage>();
            List<ComboDefinitionValidationMessage> warnings = new List<ComboDefinitionValidationMessage>();

            if (contract == null)
            {
                errors.Add(BuildMessage("ComboContractMissing", null, null, "组合技契约不存在，主模组无法读取它。"));
                return BuildResult(errors, warnings);
            }

            if (string.IsNullOrWhiteSpace(contract.FirstSourceActionDefName))
            {
                errors.Add(BuildMessage("ComboFirstSourceMissing", null, "firstSourceActionDefName", "组合技缺少第一来源动作预设声明。"));
            }

            if (string.IsNullOrWhiteSpace(contract.SecondSourceActionDefName))
            {
                errors.Add(BuildMessage("ComboSecondSourceMissing", null, "secondSourceActionDefName", "组合技缺少第二来源动作预设声明。"));
            }

            ValidateSourceAdmission(contract.FirstSourceAdmission, "FirstSourceAdmission", errors);
            ValidateSourceAdmission(contract.SecondSourceAdmission, "SecondSourceAdmission", errors);

            ValidateUseRequirements(contract.UseRequirements, errors);

            if (contract.Expression == null || !contract.Expression.HasExpressionBlock || contract.Expression.Config == null)
            {
                errors.Add(BuildMessage("ComboExpressionMissing", null, "Expression", "组合技缺少表达声明块，表达系统无法知道它能成立什么结果。"));
            }
            else if (contract.Expression.Config.Entries == null || contract.Expression.Config.Entries.Count == 0)
            {
                errors.Add(BuildMessage("ComboExpressionEmpty", null, "Expression.Entries", "组合技声明了表达块，但没有任何表达条目。"));
            }

            ValidateExpressionSustainCosts(
                contract.Expression != null ? contract.Expression.Config : null,
                errors);
            return BuildResult(errors, warnings);
        }

        /// <summary>拒绝来源准入列表中的空白身份键。</summary>
        private static void ValidateSourceAdmission(
            ComboSourceAdmissionContract admission,
            string fieldPrefix,
            List<ComboDefinitionValidationMessage> errors)
        {
            if (admission == null)
            {
                return;
            }

            ValidateIdentityList(admission.AllowedProfessions, fieldPrefix + ".AllowedProfessions", errors);
            ValidateIdentityList(admission.DeniedProfessions, fieldPrefix + ".DeniedProfessions", errors);
            ValidateIdentityList(admission.AllowedCategories, fieldPrefix + ".AllowedCategories", errors);
            ValidateIdentityList(admission.DeniedCategories, fieldPrefix + ".DeniedCategories", errors);
            ValidateIdentityList(admission.AllowedTags, fieldPrefix + ".AllowedTags", errors);
            ValidateIdentityList(admission.RequiredTags, fieldPrefix + ".RequiredTags", errors);
            ValidateIdentityList(admission.DeniedTags, fieldPrefix + ".DeniedTags", errors);
            ValidateIdentityList(admission.AllowedSourceVariants, fieldPrefix + ".AllowedSourceVariants", errors);
            ValidateIdentityList(admission.DeniedSourceVariants, fieldPrefix + ".DeniedSourceVariants", errors);
        }

        /// <summary>校验单个身份键列表。</summary>
        private static void ValidateIdentityList(
            IReadOnlyList<string> values,
            string fieldName,
            List<ComboDefinitionValidationMessage> errors)
        {
            if (values == null)
            {
                return;
            }

            for (int index = 0; index < values.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(values[index]))
                {
                    errors.Add(BuildMessage(
                        "ComboSourceAdmissionIdentityMissing",
                        null,
                        fieldName + "[" + index + "]",
                        "组合技来源准入包含空白身份键。"));
                }
            }
        }

        /// <summary>
        /// 用中性角色条件校验器检查组合技自己的使用门槛。
        /// </summary>
        private static void ValidateUseRequirements(
            IReadOnlyList<PawnRequirement> requirements,
            List<ComboDefinitionValidationMessage> errors)
        {
            IReadOnlyList<PawnRequirementValidationIssue> issues =
                PawnRequirementListValidator.Instance.Validate(requirements);
            for (int i = 0; i < issues.Count; i++)
            {
                PawnRequirementValidationIssue issue = issues[i];
                errors.Add(BuildMessage(
                    "ComboUseRequirement" + issue.Code,
                    null,
                    "UseRequirements[" + issue.Index + "]",
                    issue.Message));
            }
        }

        /// <summary>
        /// 校验组合技每条表达显式声明的持续 Trion 费用表。
        /// 组合技可以不写费用表，但不能通过另一套继承开关隐式取得它。
        /// </summary>
        private static void ValidateExpressionSustainCosts(
            ComboExpressionConfig expressionConfig,
            List<ComboDefinitionValidationMessage> errors)
        {
            if (expressionConfig?.Entries == null || errors == null)
            {
                return;
            }

            for (int i = 0; i < expressionConfig.Entries.Count; i++)
            {
                ComboExpressionEntryConfig entry = expressionConfig.Entries[i];
                if (entry == null)
                {
                    continue;
                }

                string entryId = string.IsNullOrWhiteSpace(entry.Id) ? "(未命名)" : entry.Id;
                IReadOnlyList<string> sustainErrors = ExpressionSustainCostPolicy.Validate(
                    entry.Trion != null ? entry.Trion.SustainCostBySourceCount : null,
                    "组合技条目 " + entryId);
                for (int errorIndex = 0; errorIndex < sustainErrors.Count; errorIndex++)
                {
                    errors.Add(BuildMessage(
                        "ComboExpressionSustainCostInvalid",
                        null,
                        "Expression.Entries." + entryId + ".Trion.SustainCostBySourceCount",
                        sustainErrors[errorIndex]));
                }
            }
        }

        /// <summary>
        /// 构建统一校验结果。
        /// </summary>
        private static ComboDefinitionValidationResult BuildResult(
            List<ComboDefinitionValidationMessage> errors,
            List<ComboDefinitionValidationMessage> warnings)
        {
            return new ComboDefinitionValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }

        /// <summary>
        /// 构建单条校验消息。
        /// </summary>
        private static ComboDefinitionValidationMessage BuildMessage(
            string code,
            string sourceChipDefName,
            string fieldName,
            string message)
        {
            return new ComboDefinitionValidationMessage
            {
                Code = code,
                SourceChipDefName = sourceChipDefName,
                FieldName = fieldName,
                Message = message
            };
        }
    }
}
