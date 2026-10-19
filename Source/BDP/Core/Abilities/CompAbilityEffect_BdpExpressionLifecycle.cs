using BDP.Core.Expressions;
using RimWorld;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>区分持久能力状态与当前来源资格；关闭只禁用，不重建次数池。</summary>
    public sealed class CompAbilityEffect_BdpExpressionLifecycle : CompAbilityEffect
    {
        /// <summary>来源当前仍在正式能力结果中。</summary>
        public bool SourceActive
        {
            get
            {
                FormalExpressionResult result;
                return DefaultExpressionAbilityHostSynchronizer.TryResolveBoundAbilityResult(
                    parent?.pawn, parent?.def, out result) && result.IsAvailable
                    && (result.UseRequirementCheck == null || result.UseRequirementCheck.Satisfied);
            }
        }

        /// <summary>来源停用隐藏按钮，但原版能力对象继续更新和存档。</summary>
        public override bool ShouldHideGizmo => !SourceActive;
        /// <summary>队列与作业准入仍检查来源，不能仅靠按钮隐藏。</summary>
        public override bool CanCast => SourceActive;
        /// <summary>按钮显示资格原因。</summary>
        public override bool GizmoDisabled(out string reason)
        {
            reason = SourceActive ? null : "BDP_BatchAbility_SourceInactive".Translate().ToString();
            return reason != null;
        }
        /// <summary>目标确认期间来源失效则拒绝。</summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (SourceActive) return base.Valid(target, throwMessages);
            if (throwMessages && parent?.pawn != null)
                Messages.Message("BDP_BatchAbility_SourceInactive".Translate(), parent.pawn, MessageTypeDefOf.RejectInput, false);
            return false;
        }
        /// <summary>前摇效果复查同一来源资格。</summary>
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => SourceActive;
        /// <summary>人工智能也必须具有有效来源。</summary>
        public override bool AICanTargetNow(LocalTargetInfo target) => SourceActive && base.AICanTargetNow(target);
    }
}
