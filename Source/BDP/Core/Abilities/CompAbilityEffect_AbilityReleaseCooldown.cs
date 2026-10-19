using RimWorld;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// Ability 释放冷却的通用运行时组件。
    /// 它复用原版 Ability 的实际冷却计时，同时把冷却复制到稳定的来源级账本，
    /// 使 Ability 宿主被移除后重新挂载时仍然遵守原冷却。
    /// </summary>
    public sealed class CompAbilityEffect_AbilityReleaseCooldown : CompAbilityEffect
    {
        /// <summary>
        /// 当前效果组件使用的强类型配置。
        /// </summary>
        private new CompProperties_AbilityReleaseCooldown Props
        {
            get { return (CompProperties_AbilityReleaseCooldown)props; }
        }

        /// <summary>
        /// 来源级冷却未结束时禁止再次释放。
        /// 原版 Ability 自身的冷却仍由 Ability 负责判断。
        /// </summary>
        public override bool CanCast
        {
            get
            {
                return !AbilityReleaseCooldownService.IsOnCooldown(
                    parent != null ? parent.pawn : null,
                    parent != null ? parent.def : null,
                    out int _);
            }
        }

        /// <summary>
        /// 来源级冷却仍存在时让按钮保持置灰。
        /// </summary>
        public override bool GizmoDisabled(out string reason)
        {
            int remaining;
            if (AbilityReleaseCooldownService.IsOnCooldown(
                parent != null ? parent.pawn : null,
                parent != null ? parent.def : null,
                out remaining))
            {
                reason = "AbilityOnCooldown".Translate(remaining.ToStringTicksToPeriod()).Resolve();
                return true;
            }

            reason = null;
            return false;
        }

        /// <summary>
        /// 目标确认阶段再次检查来源级冷却，防止瞄准期间绕过按钮状态。
        /// </summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            int remaining;
            if (!AbilityReleaseCooldownService.IsOnCooldown(
                parent != null ? parent.pawn : null,
                parent != null ? parent.def : null,
                out remaining))
            {
                return true;
            }

            if (throwMessages && parent?.pawn != null)
            {
                Messages.Message(
                    "AbilityOnCooldown".Translate(remaining.ToStringTicksToPeriod()).Resolve(),
                    parent.pawn,
                    MessageTypeDefOf.RejectInput,
                    false);
            }

            return false;
        }

        /// <summary>
        /// 让 AI 也遵守来源级冷却。
        /// </summary>
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return CanCast && base.AICanTargetNow(target);
        }

        /// <summary>
        /// Ability 真正执行效果时记录原版已经启动的实际剩余和总冷却时长。
        /// </summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            AbilityReleaseCooldownService.RecordReleaseCooldown(parent, Props.cooldownKey);
        }
    }
}
