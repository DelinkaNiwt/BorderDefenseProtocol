using BDP.Core.Expressions;
using RimWorld;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// Ability 一次性应用表达宿主 Hediff 的通用效果组件。
    /// 它不负责业务状态关闭，也不负责 Hediff 持续时间或 Ability 冷却。
    /// </summary>
    public sealed class CompAbilityEffect_ApplyExpressionHediff : CompAbilityEffect
    {
        /// <summary>
        /// 当前效果组件使用的强类型配置。
        /// </summary>
        private new CompProperties_AbilityEffect_ApplyExpressionHediff Props
        {
            get { return (CompProperties_AbilityEffect_ApplyExpressionHediff)props; }
        }

        /// <summary>
        /// Ability 真正释放时添加目标表达宿主 Hediff；已有实例不重复添加。
        /// </summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = parent != null ? parent.pawn : null;
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.hediffDefName);
            if (!IsExpressionHostHediffDef(hediffDef))
            {
                Log.Error(
                    "[BDP] Ability Hediff application requires an expression host HediffDef: "
                    + (Props.hediffDefName ?? "<null>"));
                return;
            }

            if (pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef, false) != null)
            {
                return;
            }

            Hediff added = pawn.health.AddHediff(hediffDef);
            if (added != null)
            {
                ExpressionSurfaceAccess.SynchronizePublishedExpressionHostsImmediately(pawn);
            }
        }

        /// <summary>
        /// 确认目标 HediffDef 使用 BDP 表达宿主类型。
        /// </summary>
        private static bool IsExpressionHostHediffDef(HediffDef hediffDef)
        {
            return hediffDef?.hediffClass != null
                && typeof(BdpExpressionHostHediff).IsAssignableFrom(hediffDef.hediffClass);
        }
    }
}
