using System.Collections.Generic;
using RimWorld;

namespace BDP.Core.Requirements
{
    /// <summary>
    /// 一组角色条件共用的定义层结构校验器。
    /// 芯片和 Combo 可以在此基础上追加各自的必填规则。
    /// </summary>
    public sealed class PawnRequirementListValidator
    {
        /// <summary>共享无状态实例。</summary>
        public static readonly PawnRequirementListValidator Instance =
            new PawnRequirementListValidator();

        /// <summary>禁止外部创建重复校验器。</summary>
        private PawnRequirementListValidator()
        {
        }

        /// <summary>检查空项、单条定义、重复释放力与重复技能条件。</summary>
        public IReadOnlyList<PawnRequirementValidationIssue> Validate(
            IReadOnlyList<PawnRequirement> requirements)
        {
            List<PawnRequirementValidationIssue> issues =
                new List<PawnRequirementValidationIssue>();
            int intensityCount = 0;
            HashSet<SkillDef> skills = new HashSet<SkillDef>();
            if (requirements == null)
            {
                return issues.AsReadOnly();
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                PawnRequirement requirement = requirements[i];
                if (requirement == null)
                {
                    issues.Add(PawnRequirementValidationIssue.Create(
                        i,
                        null,
                        "EntryMissing",
                        "条件列表存在空条目。"));
                    continue;
                }

                if (requirement is TrionIntensityRequirement)
                {
                    intensityCount++;
                    if (intensityCount > 1)
                    {
                        issues.Add(PawnRequirementValidationIssue.Create(
                            i,
                            requirement,
                            "TrionIntensityDuplicate",
                            "同一条件列表只能声明一条 Trion 释放力门槛。"));
                    }
                }

                SkillLevelRequirement skillRequirement = requirement as SkillLevelRequirement;
                if (skillRequirement?.Skill != null && !skills.Add(skillRequirement.Skill))
                {
                    issues.Add(PawnRequirementValidationIssue.Create(
                        i,
                        requirement,
                        "SkillDuplicate",
                        "同一条件列表不得重复声明相同技能的等级门槛："
                            + skillRequirement.Skill.LabelCap));
                }

                string definitionError = requirement.ValidateDefinition();
                if (!string.IsNullOrEmpty(definitionError))
                {
                    issues.Add(PawnRequirementValidationIssue.Create(
                        i,
                        requirement,
                        "DefinitionInvalid",
                        definitionError));
                }
            }

            return issues.AsReadOnly();
        }
    }

    /// <summary>
    /// 单条角色条件列表校验问题。
    /// </summary>
    public sealed class PawnRequirementValidationIssue
    {
        /// <summary>问题条件在作者列表中的零基索引。</summary>
        public int Index { get; private set; }

        /// <summary>问题对应的条件；空条目时为空。</summary>
        public PawnRequirement Requirement { get; private set; }

        /// <summary>供业务适配器映射诊断的稳定问题码。</summary>
        public string Code { get; private set; }

        /// <summary>玩家或作者可读问题说明。</summary>
        public string Message { get; private set; }

        /// <summary>创建不可变校验问题。</summary>
        internal static PawnRequirementValidationIssue Create(
            int index,
            PawnRequirement requirement,
            string code,
            string message)
        {
            return new PawnRequirementValidationIssue
            {
                Index = index,
                Requirement = requirement,
                Code = code,
                Message = message
            };
        }
    }
}
