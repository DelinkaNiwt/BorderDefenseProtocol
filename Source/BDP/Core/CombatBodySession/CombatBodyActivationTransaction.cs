using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.External;
using BDP.Core.Genes;
using BDP.Core.Trigger;
using BDP.Core.Trion;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.CombatBodySession
{
    /// <summary>
    /// 战斗体激活事务。
    /// 它负责收口进入 Active 前后的完整跨系统顺序。
    /// </summary>
    internal sealed class CombatBodyActivationTransaction
    {
        /// <summary>
        /// 战斗体宿主 comp。
        /// </summary>
        private readonly CompCombatBodyHost owner;

        /// <summary>
        /// 原始战斗体相位服务。
        /// </summary>
        private readonly CombatBodyService rawCombatBodyService;

        /// <summary>
        /// 战斗会话判断策略。
        /// </summary>
        private readonly CombatBodySessionPolicy policy;

        /// <summary>
        /// 战斗会话 `Trion` 绑定器。
        /// </summary>
        private readonly CombatBodySessionTrionBinding trionBinding;

        /// <summary>
        /// 激活完成后的状态变更通知。
        /// </summary>
        private readonly Action notifyCombatBodySessionStateChanged;

        /// <summary>
        /// 构造战斗体激活事务。
        /// </summary>
        public CombatBodyActivationTransaction(
            CompCombatBodyHost owner,
            CombatBodyService rawCombatBodyService,
            CombatBodySessionPolicy policy,
            CombatBodySessionTrionBinding trionBinding,
            Action notifyCombatBodySessionStateChanged)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.rawCombatBodyService = rawCombatBodyService ?? throw new ArgumentNullException(nameof(rawCombatBodyService));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
            this.trionBinding = trionBinding ?? throw new ArgumentNullException(nameof(trionBinding));
            this.notifyCombatBodySessionStateChanged = notifyCombatBodySessionStateChanged ?? throw new ArgumentNullException(nameof(notifyCombatBodySessionStateChanged));
        }

        /// <summary>
        /// 尝试执行战斗体激活事务。
        /// </summary>
        public bool TryActivate(Pawn ownerPawn)
        {
            long activationStart = BdpDiagnostics.BeginTiming();
            BdpDiagnostics.Trace(
                "战斗体激活"
                + " | stage=begin"
                + " | pawn=" + (ownerPawn != null ? ownerPawn.ThingID : "null")
                + " | phase=" + rawCombatBodyService.Phase);
            if (!TrionGlandEligibility.HasActiveTrionGland(ownerPawn)
                || !rawCombatBodyService.CanActivate()
                || !policy.HasOperationalTrion(ownerPawn))
            {
                BdpDiagnostics.Trace(
                    "战斗体激活"
                    + " | stage=rejected"
                    + " | pawn=" + (ownerPawn != null ? ownerPawn.ThingID : "null")
                    + " | reason=gland_or_phase_not_ready");
                return false;
            }

            if (!policy.TryResolvePrimaryTrigger(ownerPawn, out CompTriggerBody trigger))
            {
                BdpDiagnostics.Trace(
                    "战斗体激活"
                    + " | stage=rejected"
                    + " | pawn=" + (ownerPawn != null ? ownerPawn.ThingID : "null")
                    + " | reason=primary_trigger_not_resolved");
                return false;
            }

            trigger.TraceLifecycle(
                "combat_body_activation_trigger_resolved",
                "phase=" + rawCombatBodyService.Phase
                + " | specialSlots=" + trigger.DescribeSlotsForDiagnostics(TriggerSide.Special));

            ITrionCommands trionCommands = TrionSurfaceAccess.ResolveCommands(ownerPawn);
            if (trionCommands == null)
            {
                trigger.TraceLifecycle("combat_body_activation_rejected", "reason=trion_commands_missing");
                return false;
            }

            long allocationStart = BdpDiagnostics.BeginTiming();
            float allocateAmount = CalculateCombatBodyAllocateAmount(trigger);
            BdpDiagnostics.TraceTiming(
                "战斗体激活 | stage=calculate_allocate | pawn=" + ownerPawn.ThingID
                + " | allocateTrion=" + allocateAmount,
                allocationStart);
            trigger.TraceLifecycle(
                "combat_body_activation_resource_check",
                "allocateTrion=" + allocateAmount);
            if (allocateAmount > 0f && !trionCommands.CanAfford(allocateAmount))
            {
                trigger.TraceLifecycle("combat_body_activation_rejected", "reason=trion_not_affordable");
                return false;
            }

            if (allocateAmount > 0f && !trionCommands.Allocate(allocateAmount))
            {
                trigger.TraceLifecycle("combat_body_activation_rejected", "reason=trion_allocate_failed");
                return false;
            }

            try
            {
                long enterActiveStart = BdpDiagnostics.BeginTiming();
                if (!rawCombatBodyService.TryEnterActive(allocateAmount))
                {
                    BdpDiagnostics.TraceTiming(
                        "战斗体激活 | stage=enter_active_rejected | pawn=" + ownerPawn.ThingID,
                        enterActiveStart);
                    if (allocateAmount > 0f)
                    {
                        trionCommands.Release(allocateAmount);
                    }

                    trigger.TraceLifecycle("combat_body_activation_rejected", "reason=raw_enter_active_failed");
                    return false;
                }

                BdpDiagnostics.TraceTiming(
                    "战斗体激活 | stage=enter_active | pawn=" + ownerPawn.ThingID,
                    enterActiveStart);
            }
            catch
            {
                if (allocateAmount > 0f)
                {
                    trionCommands.Release(allocateAmount);
                }

                throw;
            }

            long triggerDisableStart = BdpDiagnostics.BeginTiming();
            trigger.SetCombatBodyUnavailableDisabled(false);
            BdpDiagnostics.TraceTiming(
                "战斗体激活 | stage=trigger_disable_clear | pawn=" + ownerPawn.ThingID,
                triggerDisableStart);
            CombatBodyCollapseExtensionRegistry.Clear(ownerPawn);

            long specialActivationStart = BdpDiagnostics.BeginTiming();
            TryAutoActivateSpecialSlots(trigger);
            BdpDiagnostics.TraceTiming(
                "战斗体激活 | stage=special_auto_activation | pawn=" + ownerPawn.ThingID,
                specialActivationStart);

            long primaryActivationStart = BdpDiagnostics.BeginTiming();
            TryAutoActivatePrimarySlots(ownerPawn);
            BdpDiagnostics.TraceTiming(
                "战斗体激活 | stage=primary_auto_activation | pawn=" + ownerPawn.ThingID,
                primaryActivationStart);

            long trionRuntimeStart = BdpDiagnostics.BeginTiming();
            trionBinding.BindActiveRuntime();
            BdpDiagnostics.TraceTiming(
                "战斗体激活 | stage=bind_active_runtime | pawn=" + ownerPawn.ThingID,
                trionRuntimeStart);

            long woundRuntimeStart = BdpDiagnostics.BeginTiming();
            owner.WoundRuntime.RebuildActiveWounds(ownerPawn);
            BdpDiagnostics.TraceTiming(
                "战斗体激活 | stage=rebuild_wound_runtime | pawn=" + ownerPawn.ThingID,
                woundRuntimeStart);
            trionCommands.SetFrozen(true);

            long sessionNotifyStart = BdpDiagnostics.BeginTiming();
            notifyCombatBodySessionStateChanged();
            BdpDiagnostics.TraceTiming(
                "战斗体激活 | stage=notify_session_changed | pawn=" + ownerPawn.ThingID,
                sessionNotifyStart);
            trigger.TraceSlotBoundary(
                "combat_body_activation_complete",
                "phase=" + rawCombatBodyService.Phase
                + " | specialSideContext=" + trigger.DescribeSideSwitchContextForDiagnostics(TriggerSide.Special));
            BdpDiagnostics.TraceTiming(
                "战斗体激活 | stage=total | pawn=" + ownerPawn.ThingID,
                activationStart);
            return true;
        }

        /// <summary>
        /// 计算当前战斗体激活时需要正式锁定的 `Trion` 总量。
        /// </summary>
        internal float CalculateCombatBodyAllocateAmount(CompTriggerBody trigger)
        {
            ITriggerLoadoutReader loadoutReader = trigger != null ? trigger.LoadoutReaderSurface : null;
            if (loadoutReader == null)
            {
                return 0f;
            }

            float total = 0f;
            HashSet<string> chargedRoots = new HashSet<string>();
            foreach (ITriggerSlotState slot in loadoutReader.GetAllSlots())
            {
                if (slot == null || slot.LoadedChip == null)
                {
                    continue;
                }

                if (!chargedRoots.Add(BuildCapacityChargeKey(slot)))
                {
                    continue;
                }

                ChipTrionContract chipTrion = ResolveChipTrionContract(slot.LoadedChip);
                if (chipTrion != null && chipTrion.CapacityCost > 0f)
                {
                    total += chipTrion.CapacityCost;
                }
            }

            return TrionResourceRuleRegistry.ResolveCapacityCost(trigger.OwnerPawn, total);
        }

        /// <summary>
        /// 为当前槽位生成一条占用去重 key。
        /// </summary>
        private string BuildCapacityChargeKey(ITriggerSlotState slot)
        {
            if (slot == null)
            {
                return string.Empty;
            }

            if (slot.HasBindingPartner)
            {
                return slot.BindingRootSide + ":" + slot.BindingRootIndex;
            }

            return slot.Side + ":" + slot.Index;
        }

        /// <summary>
        /// 读取某枚芯片当前声明的 `Trion` 契约。
        /// </summary>
        private static ChipTrionContract ResolveChipTrionContract(Thing chip)
        {
            ChipDefinitionReadResult readResult = ChipSurfaceAccess.Read(chip);
            if (readResult == null
                || readResult.Validation == null
                || !readResult.Validation.IsValid
                || readResult.Contract == null)
            {
                return null;
            }

            return readResult.Contract.Trion;
        }

        /// <summary>
        /// 激活成功后自动尝试启用全部 `Special` 槽位。
        /// </summary>
        private void TryAutoActivateSpecialSlots(CompTriggerBody trigger)
        {
            trigger?.AutoActivateSpecialSlots();
        }

        /// <summary>
        /// 战斗体激活成功后，尝试激活主手和副手的一号槽。
        /// 这里复用正式交互语义，避免绕过禁用、镜像和激活条件判断。
        /// </summary>
        private void TryAutoActivatePrimarySlots(Pawn ownerPawn)
        {
            ITriggerInteractionReader interactionReader =
                TriggerSurfaceAccess.ResolveInteractionReader(ownerPawn);
            ITriggerLoadoutCommands loadoutCommands =
                TriggerSurfaceAccess.ResolveLoadoutCommands(ownerPawn);
            if (interactionReader == null || loadoutCommands == null)
            {
                return;
            }

            TryAutoActivatePrimarySlot(interactionReader, loadoutCommands, TriggerSide.Main);
            TryAutoActivatePrimarySlot(interactionReader, loadoutCommands, TriggerSide.Sub);
        }

        /// <summary>
        /// 按指定侧的一号槽交互语义提交一次正式激活请求。
        /// </summary>
        private static void TryAutoActivatePrimarySlot(
            ITriggerInteractionReader interactionReader,
            ITriggerLoadoutCommands loadoutCommands,
            TriggerSide side)
        {
            ITriggerSlotInteractionState interaction = interactionReader.GetSlotInteraction(side, 0);
            if (interaction == null
                || interaction.Availability != TriggerInteractionAvailability.Available
                || (interaction.OperationKind != TriggerInteractionOperationKind.Activate
                    && interaction.OperationKind != TriggerInteractionOperationKind.SwitchTo))
            {
                return;
            }

            loadoutCommands.RequestActivate(interaction.ControlSide, interaction.ControlSlotIndex);
        }

    }
}
