using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Requirements;
using BDP.Core.Trigger.Runtime;
using RimWorld;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger owner 对已激活芯片条件的低频复查边界。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 在本 Trigger 的错峰到期刻检查全部真实激活根槽。
        /// 条件失效只提交现有正常关闭，不直接撤下或自动重开。
        /// </summary>
        internal bool CheckActiveActivationRequirementsForRuntimeTick()
        {
            IReadOnlyList<TriggerSlotState> activeRoots;
            int stableThingId = parent != null ? parent.thingIDNumber : 0;
            if (!TriggerActivationRequirementMonitor.Instance.TryCollectDueActiveRoots(
                GetCurrentTick(),
                stableThingId,
                EnumerateRawSlots(),
                GetSwitchContext,
                out activeRoots))
            {
                return false;
            }

            bool submittedAnyDeactivation = false;
            for (int i = 0; i < activeRoots.Count; i++)
            {
                TriggerSlotState rootSlot = activeRoots[i];
                PawnRequirementCheckResult result =
                    ChipActivationRequirementService.Instance.Evaluate(
                        OwnerPawn,
                        rootSlot.LoadedChip);
                if (result.Satisfied)
                {
                    continue;
                }

                if (triggerService.RequestDeactivate(BuildSwitchContext(), rootSlot.Side))
                {
                    submittedAnyDeactivation = true;
                    Messages.Message(
                        BuildContinuousRequirementFailureMessage(rootSlot.LoadedChip, result),
                        MessageTypeDefOf.RejectInput,
                        false);
                }
            }

            return submittedAnyDeactivation;
        }

        /// <summary>
        /// 把持续失效的全部原因合并成一次关闭提示。
        /// </summary>
        private static string BuildContinuousRequirementFailureMessage(
            Thing chip,
            PawnRequirementCheckResult result)
        {
            string message = "BDP_Message_Chip_ContinuousRequirementFailure".Translate(
                chip != null ? chip.LabelShortCap : "BDP_Message_Chip_Default".Translate().ToString());
            IReadOnlyList<PawnRequirementSnapshot> failures =
                result != null ? result.Failures : null;
            if (failures == null)
            {
                return message;
            }

            for (int i = 0; i < failures.Count; i++)
            {
                PawnRequirementSnapshot failure = failures[i];
                if (failure != null)
                {
                    message += "\n- " + failure.FailureReason;
                }
            }

            return message;
        }
    }
}
