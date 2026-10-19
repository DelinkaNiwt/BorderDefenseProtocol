using System.Collections.Generic;
using BDP.Core.Expressions;
using RimWorld;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// Ability 按钮切换表达宿主 Hediff 的通用效果组件。
    /// 它只操作配置指定的 BdpExpressionHostHediff，不包含任何业务芯片逻辑。
    /// </summary>
    public sealed class CompAbilityEffect_ToggleExpressionHediff : CompAbilityEffect
    {
        /// <summary>
        /// 当前效果组件使用的强类型配置。
        /// </summary>
        private new CompProperties_AbilityEffect_ToggleExpressionHediff Props
        {
            get { return (CompProperties_AbilityEffect_ToggleExpressionHediff)props; }
        }

        /// <summary>
        /// 以当前实际 Hediff 是否存在作为开关状态：存在则移除，不存在则添加。
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
                    "[BDP] Ability Hediff toggle requires an expression host HediffDef: "
                    + (Props.hediffDefName ?? "<null>"));
                return;
            }

            Hediff current = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef, false);
            if (current != null)
            {
                RemoveAllHediffsOfDef(pawn, hediffDef);
                ExpressionSurfaceAccess.SynchronizePublishedExpressionHostsImmediately(pawn);
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

        /// <summary>
        /// 移除目标 HediffDef 的全部实例，保持按钮关闭后的实际状态唯一。
        /// </summary>
        private static void RemoveAllHediffsOfDef(Pawn pawn, HediffDef hediffDef)
        {
            List<Hediff> current = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs(
                ref current,
                hediff => hediff != null && hediff.def == hediffDef);
            for (int i = 0; i < current.Count; i++)
            {
                pawn.health.RemoveHediff(current[i]);
            }
        }
    }
}
