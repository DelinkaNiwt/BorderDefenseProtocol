using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.External;
using BDP.Core.Trigger;
using BDP.Core.Trion;
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
            if (!rawCombatBodyService.CanActivate())
            {
                return false;
            }

            if (!policy.TryResolvePrimaryTrigger(ownerPawn, out CompTriggerBody trigger))
            {
                return false;
            }

            ITrionCommands trionCommands = TrionSurfaceAccess.ResolveCommands(ownerPawn);
            if (trionCommands == null)
            {
                return false;
            }

            float allocateAmount = CalculateCombatBodyAllocateAmount(trigger);
            if (allocateAmount > 0f && !trionCommands.CanAfford(allocateAmount))
            {
                return false;
            }

            if (allocateAmount > 0f && !trionCommands.Allocate(allocateAmount))
            {
                return false;
            }

            try
            {
                if (!rawCombatBodyService.TryEnterActive(allocateAmount))
                {
                    if (allocateAmount > 0f)
                    {
                        trionCommands.Release(allocateAmount);
                    }

                    return false;
                }
            }
            catch
            {
                if (allocateAmount > 0f)
                {
                    trionCommands.Release(allocateAmount);
                }

                throw;
            }

            trigger.SetCombatBodyUnavailableDisabled(false);
            CombatBodyCollapseExtensionRegistry.Clear(ownerPawn);
            TryAutoActivateSpecialSlots(trigger);
            TryAutoActivatePrimarySlots(ownerPawn);
            trionBinding.BindActiveRuntime();
            owner.WoundRuntime.RebuildActiveWounds(ownerPawn);
            trionCommands.SetFrozen(true);
            notifyCombatBodySessionStateChanged();
            return true;
        }

        /// <summary>
        /// 计算当前战斗体激活时需要正式锁定的 `Trion` 总量。
        /// </summary>
        private float CalculateCombatBodyAllocateAmount(CompTriggerBody trigger)
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

            return total;
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
            ITriggerLoadoutReader loadoutReader = trigger != null ? trigger.LoadoutReaderSurface : null;
            ITriggerLoadoutCommands triggerLoadoutCommands = trigger != null ? trigger.LoadoutCommandSurface : null;
            if (loadoutReader == null || triggerLoadoutCommands == null)
            {
                return;
            }

            foreach (ITriggerSlotState slot in loadoutReader.GetSlots(TriggerSide.Special))
            {
                if (slot == null || slot.LoadedChip == null || slot.IsDisabled || slot.IsActive)
                {
                    continue;
                }

                triggerLoadoutCommands.RequestActivate(TriggerSide.Special, slot.Index);
            }
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

