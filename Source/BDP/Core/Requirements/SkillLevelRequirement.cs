using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Requirements
{
    /// <summary>
    /// 要求角色的指定原版技能达到某个整数等级。
    /// </summary>
    public sealed class SkillLevelRequirement : PawnRequirement
    {
        /// <summary>需要检查的原版技能定义。</summary>
        public SkillDef Skill;

        /// <summary>要求的最低技能等级。</summary>
        public float MinimumLevel;

        /// <summary>玩家可读的技能名称。</summary>
        public override string Label
        {
            get { return Skill != null ? Skill.LabelCap.ToString() : "BDP_Requirement_SkillLevelLabel".Translate().ToString(); }
        }

        /// <summary>构造静态技能门槛说明。</summary>
        public override PawnRequirementSnapshot Describe()
        {
            return PawnRequirementSnapshot.Description(
                Label,
                "BDP_Requirement_SkillLevelMinimum".Translate(FormatMinimumLevel()));
        }

        /// <summary>技能引用必须有效，等级必须是 1～20 的整数。</summary>
        public override string ValidateDefinition()
        {
            if (Skill == null)
            {
                return "技能等级条件必须引用一个有效的 SkillDef。";
            }

            return MinimumLevel < 1f
                || MinimumLevel > 20f
                || float.IsNaN(MinimumLevel)
                || float.IsInfinity(MinimumLevel)
                || MinimumLevel != Mathf.Floor(MinimumLevel)
                ? "技能等级门槛必须填写 1～20 的整数。"
                : null;
        }

        /// <summary>读取角色技能记录并与门槛比较，不附加工作禁用语义。</summary>
        public override PawnRequirementSnapshot Evaluate(Pawn pawn)
        {
            SkillRecord record = Skill != null ? pawn?.skills?.GetSkill(Skill) : null;
            int minimum = Mathf.FloorToInt(MinimumLevel);
            int? current = record != null ? (int?)record.Level : null;
            bool satisfied = MeetsLevel(current, minimum);
            return PawnRequirementSnapshot.Evaluation(
                Label,
                current.HasValue ? current.Value.ToString() : "无",
                minimum.ToString(),
                satisfied,
                satisfied
                    ? null
                    : "BDP_Requirement_SkillLevelFailure".Translate(
                        Label,
                        current.HasValue ? current.Value.ToString() : "无",
                        minimum));
        }

        /// <summary>无副作用比较技能记录是否达到门槛。</summary>
        internal static bool MeetsLevel(int? currentLevel, int minimumLevel)
        {
            return currentLevel.HasValue && currentLevel.Value >= minimumLevel;
        }

        /// <summary>把合法等级格式化为整数；无效值只供定义诊断显示。</summary>
        private string FormatMinimumLevel()
        {
            return ValidateDefinition() == null
                ? Mathf.FloorToInt(MinimumLevel).ToString()
                : MinimumLevel.ToString();
        }
    }
}
