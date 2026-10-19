using BDP.Support.Diagnostics;

namespace BDP.Core.Trigger
{
    /// <summary>人类转换与永久宿主共用现成特殊槽全激活流程。</summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>沿既有交互准入尝试启用全部特殊槽，不复制芯片或绕过配置规则。</summary>
        public void AutoActivateSpecialSlots()
        {
            CompTriggerBody trigger = this;
            ITriggerLoadoutReader loadoutReader = trigger != null ? trigger.LoadoutReaderSurface : null;
            ITriggerLoadoutCommands triggerLoadoutCommands = trigger != null ? trigger.LoadoutCommandSurface : null;
            if (loadoutReader == null || triggerLoadoutCommands == null)
            {
                trigger?.TraceLifecycle("special_auto_activation_skipped", "reason=loadout_surface_missing");
                return;
            }

            trigger.TraceSlotBoundary(
                "special_auto_activation_begin",
                "specialSideContext=" + trigger.DescribeSideSwitchContextForDiagnostics(TriggerSide.Special));

            foreach (ITriggerSlotState slot in loadoutReader.GetSlots(TriggerSide.Special))
            {
                if (slot == null || slot.LoadedChip == null || slot.IsDisabled || slot.IsActive)
                {
                    BdpDiagnostics.Trace(
                        "特殊槽自动激活"
                        + " | stage=slot_skipped"
                        + " | slot=" + (slot != null ? slot.Index.ToString() : "null")
                        + " | chip=" + (slot != null && slot.LoadedChip != null ? slot.LoadedChip.ThingID : "null")
                        + " | active=" + (slot != null && slot.IsActive)
                        + " | disabled=" + (slot != null && slot.IsDisabled));
                    continue;
                }

                bool accepted = triggerLoadoutCommands.RequestActivate(TriggerSide.Special, slot.Index);
                BdpDiagnostics.Trace(
                    "特殊槽自动激活"
                    + " | stage=request_returned"
                    + " | slot=" + slot.Index
                    + " | chip=" + slot.LoadedChip.ThingID
                    + " | accepted=" + accepted
                    + " | sideContext=" + trigger.DescribeSideSwitchContextForDiagnostics(TriggerSide.Special)
                    + " | slotContext=" + trigger.DescribeSwitchContextForDiagnostics(
                        slot is TriggerSlotState concreteSlot ? concreteSlot.SlotSwitchContext : null));
            }

            trigger.TraceSlotBoundary(
                "special_auto_activation_complete",
                "specialSideContext=" + trigger.DescribeSideSwitchContextForDiagnostics(TriggerSide.Special));
        }
    }
}
