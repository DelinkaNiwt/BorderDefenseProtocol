using System;
using System.Collections.Generic;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 正式交互语义解释器。
    /// 它负责把 Trigger owner 已有真值与规则解释成对外稳定的交互语义结果。
    /// </summary>
    internal sealed class TriggerInteractionInterpreter
    {
        /// <summary>
        /// 读取指定槽位。
        /// </summary>
        private readonly Func<TriggerSide, int, TriggerSlotState> getSlot;

        /// <summary>
        /// 读取指定侧当前正式激活槽位。
        /// </summary>
        private readonly Func<TriggerSide, ITriggerSlotState> getActiveSlot;

        /// <summary>
        /// 读取指定侧当前原始激活槽位。
        /// </summary>
        private readonly Func<TriggerSide, TriggerSlotState> getActiveSlotRaw;

        /// <summary>
        /// 读取指定侧当前正式切换快照。
        /// </summary>
        private readonly Func<TriggerSide, ITriggerSwitchState> getSwitchState;

        /// <summary>
        /// 读取指定侧当前生效中的切换上下文。
        /// </summary>
        private readonly Func<TriggerSide, SwitchContext> getActiveSwitchContext;

        /// <summary>
        /// 把一个槽位归一到真正受控的控制槽位。
        /// </summary>
        private readonly Func<TriggerSlotState, TriggerSlotState> normalizeDirectControlSlot;

        /// <summary>
        /// 判断当前槽位是否需要走主副同步切换。
        /// </summary>
        private readonly Func<TriggerSlotState, bool> shouldUseSynchronizedHandTransition;

        /// <summary>
        /// 读取当前游戏刻。
        /// </summary>
        private readonly Func<int> getCurrentTick;

        /// <summary>
        /// 解析当前芯片启用延迟。
        /// </summary>
        private readonly Func<Thing, int> resolveChipActivationDelayTicks;

        /// <summary>
        /// 解析当前芯片停用延迟。
        /// </summary>
        private readonly Func<Thing, int> resolveChipDeactivationDelayTicks;

        /// <summary>
        /// 判断当前战斗体是否处于开启状态。
        /// </summary>
        private readonly Func<bool> isBattleModeActive;

        /// <summary>
        /// 对指定芯片执行当前角色的激活条件求值。
        /// </summary>
        private readonly Func<Thing, PawnRequirementCheckResult> evaluateActivationRequirements;

        /// <summary>
        /// 构造一个正式交互语义解释器。
        /// </summary>
        public TriggerInteractionInterpreter(
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<TriggerSide, ITriggerSlotState> getActiveSlot,
            Func<TriggerSide, TriggerSlotState> getActiveSlotRaw,
            Func<TriggerSide, ITriggerSwitchState> getSwitchState,
            Func<TriggerSide, SwitchContext> getActiveSwitchContext,
            Func<TriggerSlotState, TriggerSlotState> normalizeDirectControlSlot,
            Func<TriggerSlotState, bool> shouldUseSynchronizedHandTransition,
            Func<int> getCurrentTick,
            Func<Thing, int> resolveChipActivationDelayTicks,
            Func<Thing, int> resolveChipDeactivationDelayTicks,
            Func<bool> isBattleModeActive,
            Func<Thing, PawnRequirementCheckResult> evaluateActivationRequirements)
        {
            this.getSlot = getSlot;
            this.getActiveSlot = getActiveSlot;
            this.getActiveSlotRaw = getActiveSlotRaw;
            this.getSwitchState = getSwitchState;
            this.getActiveSwitchContext = getActiveSwitchContext;
            this.normalizeDirectControlSlot = normalizeDirectControlSlot;
            this.shouldUseSynchronizedHandTransition = shouldUseSynchronizedHandTransition;
            this.getCurrentTick = getCurrentTick;
            this.resolveChipActivationDelayTicks = resolveChipActivationDelayTicks;
            this.resolveChipDeactivationDelayTicks = resolveChipDeactivationDelayTicks;
            this.isBattleModeActive = isBattleModeActive;
            this.evaluateActivationRequirements = evaluateActivationRequirements;
        }

        /// <summary>
        /// 构造某个槽位当前的正式交互语义结果。
        /// </summary>
        public ITriggerSlotInteractionState GetSlotInteraction(TriggerSide side, int slotIndex, TriggerSlotState slot)
        {
            if (slot == null)
            {
                return TriggerSlotInteractionSnapshot.Create(
                    side,
                    slotIndex,
                    side,
                    slotIndex,
                    false,
                    TriggerInteractionOperationKind.Unavailable,
                    TriggerInteractionAvailability.Blocked,
                    TriggerInteractionReason.MissingSlot,
                    TriggerInteractionTransitionMode.None);
            }

            TriggerSlotState controlSlot = normalizeDirectControlSlot(slot);
            if (slot.IsBindingMirror && controlSlot != null && controlSlot != slot)
            {
                return TriggerSlotInteractionSnapshot.Create(
                    side,
                    slotIndex,
                    controlSlot.Side,
                    controlSlot.Index,
                    false,
                    TriggerInteractionOperationKind.Mirror,
                    TriggerInteractionAvailability.InformationalOnly,
                    TriggerInteractionReason.MirrorControlledByRoot,
                    TriggerInteractionTransitionMode.None);
            }

            if (slot.LoadedChip == null)
            {
                return TriggerSlotInteractionSnapshot.Create(
                    side,
                    slotIndex,
                    side,
                    slotIndex,
                    true,
                    TriggerInteractionOperationKind.Unavailable,
                    TriggerInteractionAvailability.Blocked,
                    TriggerInteractionReason.EmptySlot,
                    TriggerInteractionTransitionMode.None);
            }

            if (slot.IsDisabled)
            {
                return TriggerSlotInteractionSnapshot.Create(
                    side,
                    slotIndex,
                    side,
                    slotIndex,
                    true,
                    TriggerInteractionOperationKind.Unavailable,
                    TriggerInteractionAvailability.Blocked,
                    TriggerInteractionReason.Disabled,
                    TriggerInteractionTransitionMode.None);
            }

            if (isBattleModeActive != null && !isBattleModeActive())
            {
                return TriggerSlotInteractionSnapshot.Create(
                    side,
                    slotIndex,
                    side,
                    slotIndex,
                    true,
                    TriggerInteractionOperationKind.Unavailable,
                    TriggerInteractionAvailability.Blocked,
                    TriggerInteractionReason.BattleModeUnavailable,
                    TriggerInteractionTransitionMode.None);
            }

            if (IsControlSlotSwitching(slot))
            {
                bool isCurrentSwitchTarget = IsCurrentSwitchTarget(slot);
                if (isCurrentSwitchTarget || slot.IsActive)
                {
                    return TriggerSlotInteractionSnapshot.Create(
                        side,
                        slotIndex,
                        side,
                        slotIndex,
                        true,
                        isCurrentSwitchTarget
                            ? TriggerInteractionOperationKind.SwitchTo
                            : TriggerInteractionOperationKind.Deactivate,
                        TriggerInteractionAvailability.InformationalOnly,
                        IsWaitingTarget(slot)
                            ? TriggerInteractionReason.WaitingForConflicts
                            : TriggerInteractionReason.SwitchingInProgress,
                        ResolveActivationTransitionMode(slot));
                }
            }

            if (slot.IsActive)
            {
                PawnRequirementCheckResult activeRequirementResult =
                    EvaluateActivationRequirements(slot.LoadedChip);
                return TriggerSlotInteractionSnapshot.Create(
                    side,
                    slotIndex,
                    side,
                    slotIndex,
                    true,
                    TriggerInteractionOperationKind.Deactivate,
                    TriggerInteractionAvailability.Available,
                    TriggerInteractionReason.AlreadyActive,
                    ResolveDeactivateTransitionMode(slot),
                    activeRequirementResult.Requirements);
            }

            PawnRequirementCheckResult requirementResult =
                EvaluateActivationRequirements(slot.LoadedChip);
            bool requirementsSatisfied = requirementResult.Satisfied;
            return TriggerSlotInteractionSnapshot.Create(
                side,
                slotIndex,
                side,
                slotIndex,
                true,
                getActiveSlot(side) != null
                    ? TriggerInteractionOperationKind.SwitchTo
                    : TriggerInteractionOperationKind.Activate,
                requirementsSatisfied
                    ? TriggerInteractionAvailability.Available
                    : TriggerInteractionAvailability.Blocked,
                requirementsSatisfied
                    ? TriggerInteractionReason.None
                    : TriggerInteractionReason.ActivationRequirementsUnmet,
                ResolveActivationTransitionMode(slot),
                requirementResult.Requirements);
        }

        /// <summary>
        /// 通过 Core 唯一服务读取条件；没有求值入口时返回无条件通过结果。
        /// </summary>
        private PawnRequirementCheckResult EvaluateActivationRequirements(Thing chip)
        {
            PawnRequirementCheckResult result =
                evaluateActivationRequirements != null
                    ? evaluateActivationRequirements(chip)
                    : null;
            return result ?? new PawnRequirementCheckResult
            {
                Satisfied = true,
                Requirements = new List<PawnRequirementSnapshot>().AsReadOnly(),
                Failures = new List<PawnRequirementSnapshot>().AsReadOnly()
            };
        }

        /// <summary>
        /// 构造某一侧当前的整体交互语义结果。
        /// </summary>
        public ITriggerSideInteractionState GetSideInteraction(TriggerSide side)
        {
            TriggerSlotState activeRaw = getActiveSlotRaw(side);
            TriggerSlotState activeControl = normalizeDirectControlSlot(activeRaw);
            ITriggerSwitchState switchState = getSwitchState(side);
            bool hasActiveSlot = activeRaw != null;
            bool isSwitching = switchState.IsActive;
            int controlSlotIndex = activeControl != null ? activeControl.Index : -1;
            TriggerSide controlSide = activeControl != null ? activeControl.Side : side;

            if (isSwitching)
            {
                return TriggerSideInteractionSnapshot.Create(
                    side,
                    controlSide,
                    controlSlotIndex,
                    hasActiveSlot,
                    true,
                    switchState.TargetSlotIndex,
                    TriggerInteractionOperationKind.SwitchTo,
                    TriggerInteractionAvailability.InformationalOnly,
                    TriggerInteractionReason.SwitchingInProgress,
                    ResolveSwitchTransitionMode(side));
            }

            if (activeRaw != null && activeControl != null && (activeControl.Side != side || activeControl.Index != activeRaw.Index))
            {
                return TriggerSideInteractionSnapshot.Create(
                    side,
                    activeControl.Side,
                    activeControl.Index,
                    true,
                    false,
                    -1,
                    TriggerInteractionOperationKind.Mirror,
                    TriggerInteractionAvailability.InformationalOnly,
                    TriggerInteractionReason.MirrorControlledByRoot,
                    TriggerInteractionTransitionMode.None);
            }

            if (activeControl != null)
            {
                return TriggerSideInteractionSnapshot.Create(
                    side,
                    activeControl.Side,
                    activeControl.Index,
                    true,
                    false,
                    -1,
                    TriggerInteractionOperationKind.Deactivate,
                    TriggerInteractionAvailability.Available,
                    TriggerInteractionReason.AlreadyActive,
                    ResolveDeactivateTransitionMode(activeControl));
            }

            return TriggerSideInteractionSnapshot.Create(
                side,
                side,
                -1,
                false,
                false,
                -1,
                TriggerInteractionOperationKind.None,
                TriggerInteractionAvailability.InformationalOnly,
                TriggerInteractionReason.NoFormalAction,
                TriggerInteractionTransitionMode.None);
        }

        /// <summary>
        /// 判断当前控制槽位是否正处于切换过程中。
        /// 双手同步切换时主副任一侧存在切换都算成立。
        /// </summary>
        private bool IsControlSlotSwitching(TriggerSlotState slot)
        {
            TriggerSlotState controlSlot = normalizeDirectControlSlot(slot);
            if (controlSlot == null)
            {
                return false;
            }

            if (shouldUseSynchronizedHandTransition(controlSlot))
            {
                return getActiveSwitchContext(TriggerSide.Main) != null || getActiveSwitchContext(TriggerSide.Sub) != null;
            }

            return getActiveSwitchContext(controlSlot.Side) != null;
        }

        /// <summary>
        /// 判断当前槽位是否正是切换中的目标槽位。
        /// </summary>
        private bool IsCurrentSwitchTarget(TriggerSlotState slot)
        {
            if (slot == null)
            {
                return false;
            }

            TriggerSlotState controlSlot = normalizeDirectControlSlot(slot);
            if (controlSlot == null)
            {
                return false;
            }

            ITriggerSwitchState switchState = getSwitchState(controlSlot.Side);
            return switchState.IsActive && switchState.TargetSlotIndex == controlSlot.Index;
        }

        /// <summary>
        /// 判断当前槽位是否是尚在等待冲突者关闭的目标。
        /// 目标可以处于显式等待阶段，也可以挂在同侧旧芯片的关闭阶段上。
        /// </summary>
        private bool IsWaitingTarget(TriggerSlotState slot)
        {
            if (!IsCurrentSwitchTarget(slot))
            {
                return false;
            }

            TriggerSlotState controlSlot = normalizeDirectControlSlot(slot);
            ITriggerSwitchState switchState = controlSlot != null
                ? getSwitchState(controlSlot.Side)
                : null;
            return switchState != null
                && (switchState.Phase == SwitchPhase.WaitingForConflicts
                    || switchState.Phase == SwitchPhase.Deactivating);
        }

        /// <summary>
        /// 解析当前槽位若被激活时的正式过渡模式。
        /// </summary>
        private TriggerInteractionTransitionMode ResolveActivationTransitionMode(TriggerSlotState slot)
        {
            TriggerSlotState controlSlot = normalizeDirectControlSlot(slot);
            if (controlSlot == null)
            {
                return TriggerInteractionTransitionMode.None;
            }

            if (shouldUseSynchronizedHandTransition(controlSlot))
            {
                return TriggerInteractionTransitionMode.SynchronizedHandsSwitch;
            }

            TriggerSlotState currentActive = getActiveSlot(controlSlot.Side) as TriggerSlotState;
            SwitchContext switchContext = currentActive != null
                ? TriggerSwitchTransitionService.BuildDeactivatingContext(
                    resolveChipDeactivationDelayTicks != null ? resolveChipDeactivationDelayTicks(currentActive.LoadedChip) : 0,
                    getCurrentTick(),
                    controlSlot.Index,
                    currentActive.Index,
                    controlSlot.LoadedChip != null ? controlSlot.LoadedChip.ThingID : null)
                : TriggerSwitchTransitionService.BuildActivatingContext(
                    resolveChipActivationDelayTicks != null ? resolveChipActivationDelayTicks(controlSlot.LoadedChip) : 0,
                    getCurrentTick(),
                    controlSlot.Index,
                    controlSlot.LoadedChip != null ? controlSlot.LoadedChip.ThingID : null);
            return switchContext == null
                ? TriggerInteractionTransitionMode.Immediate
                : TriggerInteractionTransitionMode.SingleSideSwitch;
        }

        /// <summary>
        /// 解析当前槽位若被关闭时的正式过渡模式。
        /// </summary>
        private TriggerInteractionTransitionMode ResolveDeactivateTransitionMode(TriggerSlotState slot)
        {
            TriggerSlotState controlSlot = normalizeDirectControlSlot(slot);
            if (controlSlot == null)
            {
                return TriggerInteractionTransitionMode.None;
            }

            if (shouldUseSynchronizedHandTransition(controlSlot))
            {
                return TriggerInteractionTransitionMode.SynchronizedHandsSwitch;
            }

            SwitchContext switchContext = TriggerSwitchTransitionService.BuildDeactivatingContext(
                resolveChipDeactivationDelayTicks != null ? resolveChipDeactivationDelayTicks(controlSlot.LoadedChip) : 0,
                getCurrentTick(),
                -1,
                controlSlot.Index,
                null);
            return switchContext == null
                ? TriggerInteractionTransitionMode.Immediate
                : TriggerInteractionTransitionMode.SingleSideSwitch;
        }

        /// <summary>
        /// 解析当前某一侧正在进行的切换所对应的正式过渡模式。
        /// </summary>
        private TriggerInteractionTransitionMode ResolveSwitchTransitionMode(TriggerSide side)
        {
            SwitchContext switchContext = getActiveSwitchContext(side);
            if (switchContext == null)
            {
                return TriggerInteractionTransitionMode.None;
            }

            if ((side == TriggerSide.Main || side == TriggerSide.Sub)
                && getActiveSwitchContext(TriggerSide.Main) != null
                && getActiveSwitchContext(TriggerSide.Sub) != null)
            {
                return TriggerInteractionTransitionMode.SynchronizedHandsSwitch;
            }

            return TriggerInteractionTransitionMode.SingleSideSwitch;
        }
    }
}
