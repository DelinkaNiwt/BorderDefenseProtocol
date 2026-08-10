using BDP.Core.Combos;
using BDP.Core.Expressions;
using BDP.Core.Requirements;
using RimWorld;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// 表达 Ability 的 Combo 使用条件适配器。
    /// 它只负责按钮态与施放前复查，不承担 Trion 扣费或冷却。
    /// </summary>
    public sealed class CompAbilityEffect_BdpExpressionUseRequirements : CompAbilityEffect
    {
        /// <summary>让原版 Ability 总准入同时服从当前 Combo 使用条件。</summary>
        public override bool CanCast
        {
            get
            {
                PawnRequirementCheckResult check = EvaluateCurrentRequirements();
                return check == null || check.Satisfied;
            }
        }

        /// <summary>条件不足时保留 Ability 按钮，但置灰并显示全部原因。</summary>
        public override bool GizmoDisabled(out string reason)
        {
            PawnRequirementCheckResult check = EvaluateCurrentRequirements();
            if (check != null && !check.Satisfied)
            {
                reason = ComboUseRequirementService.Instance.BuildFailureText(check);
                return true;
            }

            reason = null;
            return false;
        }

        /// <summary>目标确认阶段实时复查，防止条件在瞄准期间变化后绕过按钮。</summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            PawnRequirementCheckResult check = EvaluateCurrentRequirements();
            if (check == null || check.Satisfied)
            {
                return true;
            }

            if (throwMessages)
            {
                ShowRejectMessage(ComboUseRequirementService.Instance.BuildFailureText(check));
            }

            return false;
        }

        /// <summary>让 AI 使用 Ability 时遵守同一套 Combo 条件。</summary>
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            PawnRequirementCheckResult check = EvaluateCurrentRequirements();
            return (check == null || check.Satisfied) && base.AICanTargetNow(target);
        }

        /// <summary>从当前 Ability 绑定结果找到 Combo，并实时执行共享角色条件。</summary>
        private PawnRequirementCheckResult EvaluateCurrentRequirements()
        {
            FormalExpressionResult result;
            bool found = DefaultExpressionAbilityHostSynchronizer.TryResolveBoundAbilityResult(
                parent != null ? parent.pawn : null,
                parent != null ? parent.def : null,
                out result);
            return found && !string.IsNullOrWhiteSpace(result?.ComboDefName)
                ? ComboUseRequirementService.Instance.Evaluate(parent.pawn, result.ComboDefName)
                : null;
        }

        /// <summary>用原版拒绝消息样式说明本次施放为何被拦截。</summary>
        private void ShowRejectMessage(string message)
        {
            if (parent?.pawn == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Messages.Message(message, parent.pawn, MessageTypeDefOf.RejectInput, false);
        }
    }
}
