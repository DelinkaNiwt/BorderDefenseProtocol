using BDP.Core.Trion.Intensity;
using UnityEngine;
using Verse;

namespace BDP.Core.Requirements
{
    /// <summary>
    /// 要求角色当前 Trion 释放力达到指定整数门槛。
    /// </summary>
    public sealed class TrionIntensityRequirement : PawnRequirement
    {
        /// <summary>芯片要求的最低 Trion 释放力。</summary>
        public float Minimum;

        /// <summary>玩家可读条件名称。</summary>
        public override string Label
        {
            get { return "BDP_Requirement_TrionIntensityLabel".Translate(); }
        }

        /// <summary>构造静态门槛说明。</summary>
        public override PawnRequirementSnapshot Describe()
        {
            return PawnRequirementSnapshot.Description(
                Label,
                "BDP_Requirement_TrionIntensityMinimum".Translate(FormatMinimum()));
        }

        /// <summary>释放力门槛必须是有限正整数。</summary>
        public override string ValidateDefinition()
        {
            return Minimum <= 0f
                || float.IsNaN(Minimum)
                || float.IsInfinity(Minimum)
                || Minimum != Mathf.Floor(Minimum)
                ? "Trion释放力门槛必须填写大于零的整数。"
                : null;
        }

        /// <summary>读取角色当前有效释放力并与门槛比较。</summary>
        public override PawnRequirementSnapshot Evaluate(Pawn pawn)
        {
            int current = TrionIntensityUtility.GetEffective(pawn);
            int minimum = Mathf.FloorToInt(Minimum);
            bool satisfied = current >= minimum;
            return PawnRequirementSnapshot.Evaluation(
                Label,
                TrionIntensityUtility.FormatLevel(current),
                TrionIntensityUtility.FormatLevel(minimum),
                satisfied,
                satisfied
                    ? null
                    : "BDP_Requirement_TrionIntensityFailure".Translate(
                        Label,
                        TrionIntensityUtility.FormatLevel(current),
                        TrionIntensityUtility.FormatLevel(minimum)));
        }

        /// <summary>把合法门槛格式化为整数；无效值只供定义诊断显示。</summary>
        private string FormatMinimum()
        {
            return ValidateDefinition() == null
                ? TrionIntensityUtility.FormatLevel(Mathf.FloorToInt(Minimum))
                : Minimum.ToString();
        }
    }
}
