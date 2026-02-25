using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Ability的Trion消耗CompProperties。
    /// XML中配置在AbilityDef.comps列表中。
    /// </summary>
    public class CompProperties_AbilityTrionCost : CompProperties_AbilityEffect
    {
        /// <summary>使用Ability时一次性消耗的Trion量。</summary>
        public float trionCostPerUse = 0f;

        public CompProperties_AbilityTrionCost()
        {
            compClass = typeof(CompAbilityEffect_TrionCost);
        }
    }

    /// <summary>
    /// Ability的Trion消耗效果组件——Apply()时消耗Trion，
    /// GizmoDisabled()和CanApplyOn()检查Trion可用量。
    /// 与CompAbilityEffect_Teleport等原版效果组件配合使用。
    /// </summary>
    public class CompAbilityEffect_TrionCost : CompAbilityEffect
    {
        public new CompProperties_AbilityTrionCost Props
            => (CompProperties_AbilityTrionCost)props;

        /// <summary>执行效果时消耗Trion。</summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var trion = parent.pawn?.GetComp<CompTrion>();
            if (trion != null && Props.trionCostPerUse > 0f)
                trion.Consume(Props.trionCostPerUse);
        }

        /// <summary>检查Trion是否足够（供Gizmo灰显）。</summary>
        public override bool GizmoDisabled(out string reason)
        {
            var trion = parent.pawn?.GetComp<CompTrion>();
            if (trion == null || trion.Available < Props.trionCostPerUse)
            {
                reason = $"Trion不足（需要{Props.trionCostPerUse}，可用{trion?.Available ?? 0f:F1}）";
                return true;
            }
            reason = null;
            return false;
        }

        /// <summary>前置条件检查：Trion可用量是否足够。</summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var trion = parent.pawn?.GetComp<CompTrion>();
            if (trion == null || trion.Available < Props.trionCostPerUse)
            {
                if (throwMessages)
                    Messages.Message($"Trion不足（需要{Props.trionCostPerUse}）",
                        parent.pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }
    }
}
