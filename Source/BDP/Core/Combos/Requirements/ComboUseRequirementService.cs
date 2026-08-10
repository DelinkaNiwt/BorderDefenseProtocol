using System.Collections.Generic;
using System.Text;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// Combo 使用条件的业务适配器。
    /// 它只负责找到 ComboDef；具体条件描述与求值统一交给中性角色条件底层。
    /// </summary>
    public sealed class ComboUseRequirementService
    {
        /// <summary>共享无状态服务实例。</summary>
        public static readonly ComboUseRequirementService Instance =
            new ComboUseRequirementService();

        /// <summary>禁止外部创建重复服务。</summary>
        private ComboUseRequirementService()
        {
        }

        /// <summary>按定义顺序读取组合技的全部静态条件说明。</summary>
        public IReadOnlyList<PawnRequirementSnapshot> Describe(ComboDef comboDef)
        {
            return PawnRequirementEvaluator.Instance.Describe(ResolveRequirements(comboDef));
        }

        /// <summary>按定义顺序检查组合技的全部使用条件。</summary>
        public PawnRequirementCheckResult Evaluate(Pawn pawn, ComboDef comboDef)
        {
            return PawnRequirementEvaluator.Instance.Evaluate(
                pawn,
                ResolveRequirements(comboDef));
        }

        /// <summary>按 DefName 查找组合技并检查其全部使用条件。</summary>
        public PawnRequirementCheckResult Evaluate(Pawn pawn, string comboDefName)
        {
            ComboDef comboDef = !string.IsNullOrWhiteSpace(comboDefName)
                ? DefDatabase<ComboDef>.GetNamedSilentFail(comboDefName)
                : null;
            return Evaluate(pawn, comboDef);
        }

        /// <summary>把全部失败原因合并为按钮和消息都能直接使用的文本。</summary>
        public string BuildFailureText(PawnRequirementCheckResult result)
        {
            if (result?.Failures == null || result.Failures.Count == 0)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.Failures.Count; i++)
            {
                string reason = result.Failures[i]?.FailureReason;
                if (string.IsNullOrWhiteSpace(reason))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(reason);
            }

            return builder.Length > 0 ? builder.ToString() : null;
        }

        /// <summary>读取组合技定义中的角色条件集合，并浅复制公开边界。</summary>
        private static IReadOnlyList<PawnRequirement> ResolveRequirements(ComboDef comboDef)
        {
            return comboDef?.UseRequirements != null
                ? new List<PawnRequirement>(comboDef.UseRequirements).AsReadOnly()
                : new List<PawnRequirement>().AsReadOnly();
        }
    }
}
