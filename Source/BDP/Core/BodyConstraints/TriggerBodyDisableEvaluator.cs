using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.BodyConstraints
{
    /// <summary>
    /// Trigger 局部禁用评估器。
    /// 它负责把身体缺失事实折算成 Trigger 侧别下的最小禁用结果，但不持有真值。
    /// </summary>
    internal static class TriggerBodyDisableEvaluator
    {
        /// <summary>
        /// 评估指定 Trigger 侧当前是否应因身体约束而禁用。
        /// </summary>
        public static TriggerDisableReason EvaluateSideDisableReason(Pawn ownerPawn, TriggerSide side)
        {
            if (side == TriggerSide.Special)
            {
                return TriggerDisableReason.None;
            }

            if (ownerPawn?.health?.hediffSet == null)
            {
                return TriggerDisableReason.None;
            }

            foreach (Hediff hediff in ownerPawn.health.hediffSet.hediffs)
            {
                Hediff_MissingPart missingPart = hediff as Hediff_MissingPart;
                if (missingPart?.Part == null)
                {
                    continue;
                }

                TriggerBodyPartSemanticResult semanticResult = TriggerBodyPartSemanticResolver.Resolve(missingPart.Part);
                if (semanticResult.CanDisableTrigger && semanticResult.ResolvedSide.Value == side)
                {
                    return TriggerDisableReason.MissingRequiredBodyPart;
                }
            }

            return TriggerDisableReason.None;
        }
    }
}

