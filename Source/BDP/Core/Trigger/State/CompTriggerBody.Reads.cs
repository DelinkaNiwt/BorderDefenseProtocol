using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using BDP.Core.VerbHosting;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体正式读取片段。
    /// 负责对外读取槽位、切换与容器状态。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 读取全部槽位，顺序固定为主侧、副侧、特殊侧。
        /// </summary>
        internal IEnumerable<ITriggerSlotState> GetAllSlots()
        {
            EnsureSlots();

            foreach (TriggerSlotState slot in mainSlots)
            {
                yield return slot;
            }

            foreach (TriggerSlotState slot in subSlots)
            {
                yield return slot;
            }

            foreach (TriggerSlotState slot in specialSlots)
            {
                yield return slot;
            }
        }

        /// <summary>
        /// 按侧读取槽位。
        /// </summary>
        internal IEnumerable<ITriggerSlotState> GetSlots(TriggerSide side)
        {
            foreach (TriggerSlotState slot in GetRawSlots(side))
            {
                yield return slot;
            }
        }

        /// <summary>
        /// 读取当前所有正式激活槽位。
        /// </summary>
        internal IEnumerable<ITriggerSlotState> GetActiveSlots()
        {
            foreach (ITriggerSlotState slot in GetAllSlots())
            {
                if (slot.IsActive)
                {
                    yield return slot;
                }
            }
        }

        /// <summary>
        /// 读取某一侧当前正式激活槽位。
        /// </summary>
        internal ITriggerSlotState GetActiveSlot(TriggerSide side)
        {
            foreach (TriggerSlotState slot in GetSlots(side))
            {
                if (slot.IsActive)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// 读取某一侧当前正在切换到的目标槽位。
        /// </summary>
        internal ITriggerSlotState GetActivatingSlot(TriggerSide side)
        {
            SwitchContext context = GetActiveSwitchContext(side);
            int index = context != null ? context.targetSlotIndex : -1;
            if (index < 0)
            {
                return null;
            }

            return GetSlot(side, index);
        }

        /// <summary>
        /// 读取某一侧当前切换状态快照。
        /// </summary>
        internal ITriggerSwitchState GetSwitchState(TriggerSide side)
        {
            SwitchContext context = GetActiveSwitchContext(side);
            return TriggerSwitchStateSnapshot.FromContext(context);
        }

        /// <summary>
        /// 读取某枚芯片当前正式形态键。
        /// </summary>
        internal string GetChipModeKey(Thing chip)
        {
            TriggerSlotState rootSlot = FindActiveRootSlotForChip(chip);
            if (rootSlot == null
                || !TriggerChipModeService.IsModeKeyValid(chip, rootSlot.CurrentModeKey))
            {
                return null;
            }

            return rootSlot.CurrentModeKey;
        }

        /// <summary>
        /// 读取某枚芯片当前形态内部的正式姿态键。
        /// 根槽姿态真值建立前保持为空，避免解释器猜测业务默认值。
        /// </summary>
        internal string GetChipStanceKey(Thing chip)
        {
            TriggerSlotState rootSlot = FindActiveRootSlotForChip(chip);
            if (rootSlot == null
                || !TriggerChipModeService.IsStanceKeyValid(
                    chip,
                    rootSlot.CurrentModeKey,
                    rootSlot.CurrentStanceKey))
            {
                return null;
            }

            return rootSlot.CurrentStanceKey;
        }

        /// <summary>
        /// 读取某枚正式启用芯片当前形态内的有序姿态选项。
        /// </summary>
        internal IReadOnlyList<ChipStanceOptionSnapshot> GetChipStanceOptions(Thing chip)
        {
            TriggerSlotState rootSlot = FindActiveRootSlotForChip(chip);
            return rootSlot != null
                ? TriggerChipModeService.BuildStanceOptions(chip, rootSlot.CurrentModeKey)
                : System.Array.Empty<ChipStanceOptionSnapshot>();
        }

        /// <summary>
        /// 读取某枚正式启用多形态芯片的有序形态选项。
        /// </summary>
        internal IReadOnlyList<ChipModeOptionSnapshot> GetChipModeOptions(Thing chip)
        {
            TriggerSlotState rootSlot = FindActiveRootSlotForChip(chip);
            if (rootSlot == null
                || !TriggerChipModeService.IsModeKeyValid(chip, rootSlot.CurrentModeKey))
            {
                return System.Array.Empty<ChipModeOptionSnapshot>();
            }

            return TriggerChipModeService.BuildOptions(chip);
        }

        /// <summary>
        /// 按芯片实体查找正式启用的根槽位。
        /// 成对镜像会统一归一到根槽，避免同一枚芯片产生两份形态真值。
        /// </summary>
        private TriggerSlotState FindActiveRootSlotForChip(Thing chip)
        {
            if (chip == null)
            {
                return null;
            }

            foreach (TriggerSlotState slot in EnumerateRawSlots())
            {
                if (slot == null || slot.LoadedChip != chip)
                {
                    continue;
                }

                TriggerSlotState rootSlot = NormalizeDirectControlSlot(slot);
                if (rootSlot != null
                    && rootSlot.IsActive
                    && !rootSlot.IsBindingMirror
                    && rootSlot.LoadedChip == chip)
                {
                    return rootSlot;
                }
            }

            return null;
        }

        /// <summary>
        /// 读取全部槽位的正式交互语义结果。
        /// </summary>
        internal IEnumerable<ITriggerSlotInteractionState> GetAllSlotInteractions()
        {
            foreach (TriggerSlotState slot in EnumerateRawSlots())
            {
                yield return InteractionInterpreter.GetSlotInteraction(slot.Side, slot.Index, slot);
            }
        }

        /// <summary>
        /// 按侧读取槽位交互语义结果。
        /// </summary>
        internal IEnumerable<ITriggerSlotInteractionState> GetSlotInteractions(TriggerSide side)
        {
            foreach (TriggerSlotState slot in GetRawSlots(side))
            {
                yield return InteractionInterpreter.GetSlotInteraction(slot.Side, slot.Index, slot);
            }
        }

        /// <summary>
        /// 读取某个指定槽位当前的交互语义结果。
        /// </summary>
        internal ITriggerSlotInteractionState GetSlotInteraction(TriggerSide side, int slotIndex)
        {
            return InteractionInterpreter.GetSlotInteraction(side, slotIndex, GetSlot(side, slotIndex));
        }

        /// <summary>
        /// 读取某一侧当前的整体交互语义结果。
        /// </summary>
        internal ITriggerSideInteractionState GetSideInteraction(TriggerSide side)
        {
            return InteractionInterpreter.GetSideInteraction(side);
        }

        /// <summary>
        /// 为正式投影构建检查指定侧当前槽位引用与正式容器是否一致。
        /// 这条路径只读取 owner 已成立的真值，不触发公共读路径协调。
        /// </summary>
        private bool IsContainerConsistentForProjectionBuild(TriggerSide side)
        {
            EnsureChipContainer();
            foreach (TriggerSlotState slot in GetRawSlots(side))
            {
                if (slot == null || slot.LoadedChip == null)
                {
                    continue;
                }

                if (!IsActuallyInChipContainer(slot.LoadedChip))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 在统一 runtime tick 中按需结算到期切换上下文。
        /// 这里返回“是否有运行时真值变化”，供协调器避免无意义重建。
        /// </summary>
        internal bool ResolveDueSwitchTransitionsForRuntimeTick()
        {
            if (isRestoringPostLoad)
            {
                return false;
            }

            int stampBefore = CaptureRuntimeStateStamp();
            ResolveDueSwitchTransitions();
            return stampBefore != CaptureRuntimeStateStamp();
        }

        /// <summary>
        /// 指令前强制重扫禁用状态，再结算到期切换。
        /// </summary>
        private void PrepareCommandState()
        {
            ForceSyncDisabledStateFromOwnerPawn();
            ResolveDueSwitchTransitions();
        }

        /// <summary>
        /// 装卸芯片前只强制重扫禁用状态，不在这里推进切换。
        /// </summary>
        private void PrepareLoadoutCommandState()
        {
            ForceSyncDisabledStateFromOwnerPawn();
        }

        /// <summary>
        /// 推进所有已到时点的切换状态。
        /// 这里只结算已经到期的切换；真正的宿主刷新跟随正式提交通知发生。
        /// </summary>
        private void ResolveDueSwitchTransitions()
        {
            TriggerSwitchTransitionService.ResolveDueSwitchTransitions(
                GetCurrentTick(),
                triggerService.ResolveChipActivationDelayTicks,
                triggerService.ResolveChipDeactivationDelayTicks,
                GetSwitchContext,
                SetSwitchContext,
                GetSlot,
                GetActiveSlotRaw,
                target => triggerService.FindActivationBlockers(target, GetActiveSlotsRaw(), GetSlot),
                IsPendingTargetValid,
                slot => TriggerSwitchTransitionService.DeactivateBoundSlotImmediate(slot, GetSlot, SetSwitchContext, NotifySlotDeactivated),
                null,
                NotifySlotActivationCommitted,
                NotifySlotDeactivated);
        }

        /// <summary>
        /// 采样当前 Trigger runtime 相关真值，用于判断本轮 tick 是否真的改动了状态。
        /// 这里只覆盖禁用、激活与切换上下文，不把表达构建结果本身纳入比较。
        /// </summary>
        private int CaptureRuntimeStateStamp()
        {
            EnsureSlots();

            unchecked
            {
                int hash = 17;
                hash = CombineRuntimeStateStamp(hash, CaptureSwitchContextRuntimeStamp(mainSwitchContext));
                hash = CombineRuntimeStateStamp(hash, CaptureSwitchContextRuntimeStamp(subSwitchContext));
                hash = CombineRuntimeStateStamp(hash, CaptureSwitchContextRuntimeStamp(specialSwitchContext));

                foreach (TriggerSlotState slot in EnumerateRawSlots())
                {
                    if (slot == null)
                    {
                        hash = CombineRuntimeStateStamp(hash, 0);
                        continue;
                    }

                    hash = CombineRuntimeStateStamp(hash, (int)slot.Side);
                    hash = CombineRuntimeStateStamp(hash, slot.Index);
                    hash = CombineRuntimeStateStamp(hash, slot.IsActive ? 1 : 0);
                    hash = CombineRuntimeStateStamp(hash, slot.IsDisabled ? 1 : 0);
                    hash = CombineRuntimeStateStamp(hash, (int)slot.DisabledReason);
                }

                return hash;
            }
        }

        /// <summary>
        /// 采样单侧切换上下文当前的最小 runtime 指纹。
        /// </summary>
        private static int CaptureSwitchContextRuntimeStamp(SwitchContext context)
        {
            if (context == null)
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;
                hash = CombineRuntimeStateStamp(hash, (int)context.phase);
                hash = CombineRuntimeStateStamp(hash, context.phaseEndTick);
                hash = CombineRuntimeStateStamp(hash, context.targetSlotIndex);
                hash = CombineRuntimeStateStamp(
                    hash,
                    string.IsNullOrEmpty(context.targetChipThingId)
                        ? 0
                        : context.targetChipThingId.GetHashCode());
                hash = CombineRuntimeStateStamp(hash, context.deactivatingSlotIndex);
                hash = CombineRuntimeStateStamp(hash, context.activationDelayDuration);
                hash = CombineRuntimeStateStamp(hash, context.deactivationDelayDuration);
                return hash;
            }
        }

        /// <summary>
        /// 把一个整数值并入 runtime 指纹哈希。
        /// </summary>
        private static int CombineRuntimeStateStamp(int hash, int value)
        {
            unchecked
            {
                return (hash * 31) + value;
            }
        }

        /// <summary>
        /// 从宿主 Pawn 同步禁用状态到各槽位。
        /// </summary>
        private void SyncDisabledStateFromOwnerPawn(bool forceRescan)
        {
            EnsureSlots();
            TriggerDisableSync.SyncDisabledStateFromOwnerPawn(
                OwnerPawn,
                disableSyncCache,
                GetRawSlots,
                GetSwitchContext,
                SetSwitchContext,
                slot => TriggerSwitchTransitionService.DeactivateBoundSlotImmediate(slot, GetSlot, SetSwitchContext, NotifySlotDeactivated),
                ResolveExternalDisableReason,
                NotifySlotDisableStateChanged,
                forceRescan);
        }

        /// <summary>
        /// 立即按宿主 Pawn 当前身体事实强制重算一次禁用状态。
        /// 这条路径服务事实事件落地，不依赖 runtime tick。
        /// </summary>
        private bool ForceSyncDisabledStateFromOwnerPawn()
        {
            int stampBefore = CaptureRuntimeStateStamp();
            SyncDisabledStateFromOwnerPawn(true);
            return stampBefore != CaptureRuntimeStateStamp();
        }

        /// <summary>
        /// 读取当前来自 CombatBody 的额外禁用原因。
        /// 它只复用现有禁用态，不引入另一套并列状态。
        /// </summary>
        private TriggerDisableReason ResolveExternalDisableReason(TriggerSide side)
        {
            return combatBodyUnavailableDisable ? TriggerDisableReason.CombatBodyUnavailable : TriggerDisableReason.None;
        }

        /// <summary>
        /// 读取当前游戏刻，未进入地图时返回 0。
        /// </summary>
        private int GetCurrentTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        /// <summary>
        /// 快速判断当前是否有活跃的远程武装芯片。
        /// 仅做纯读，不触发任何重算。
        /// </summary>
        internal bool HasActiveRangedChip()
        {
            var projection = PublishedCombatProjection;
            if (projection?.Snapshot == null)
                return false;
            var ranged = projection.Snapshot.PrimaryRanged;
            return ranged != null && ranged.IsAvailable && ranged.VerbProps != null;
        }

        /// <summary>
        /// 当前挂起的远程攻击目标提示。
        /// 由 GetRangedAttackAction Prefix 设置，在 TryGetActiveRangedPrimaryVerb 中消费一次后清空。
        /// 使属性级 PrimaryVerb 在无参数约束下仍能按目标射程选择正确 Verb。
        /// </summary>
        private FormalExpressionResult pendingRangedTargetResultHint;

        /// <summary>
        /// 为即将发生的 GetRangedAttackAction 射程检查准备 target 感知的 Verb 选择。
        /// 外部补丁在 PrimaryVerb 被查询前调用此方法，设置本次应返回的最优 Verb 结果标识。
        /// </summary>
        internal void PrepareRangedVerbForTarget(Thing target)
        {
            pendingRangedTargetResultHint = null;
            if (target == null)
                return;

            var projection = PublishedCombatProjection;
            if (projection?.Snapshot == null)
                return;

            var snapshot = projection.Snapshot;
            var defaultResult = snapshot.PrimaryRanged;
            if (defaultResult == null
                || defaultResult.CompositeKind == CompositeExpressionKind.DualWeapon)
                return;

            var best = AttackExecutionSurfaceAccess.SelectSingleSideRangedByRangePublic(
                snapshot, OwnerPawn, target);
            if (best != null && best != defaultResult)
                pendingRangedTargetResultHint = best;
        }

        /// <summary>
        /// 获取当前活跃的远程主攻 formal host verb。
        /// 优先检查挂起的 target hint（来自 GetRangedAttackAction Prefix），
        /// 没有 hint 时回退到默认 PrimaryRanged。
        /// hint 由外部 Postfix 显式清除，不在此处消费。
        /// 供补丁桥接到 CompEquippable.PrimaryVerb 时使用。
        /// </summary>
        internal Verb TryGetActiveRangedPrimaryVerb()
        {
            if (!HasActiveRangedChip())
                return null;

            // 优先使用 target hint，但不消费（GetRangedAttackAction 内多次调用 PrimaryVerb）
            FormalExpressionResult effectiveResult = pendingRangedTargetResultHint;
            if (effectiveResult == null)
                effectiveResult = PublishedCombatProjection.Snapshot.PrimaryRanged;

            if (string.IsNullOrWhiteSpace(effectiveResult?.Id))
                return null;
            if (VerbHostSurfaceAccess.TryGetByResultId(OwnerPawn, effectiveResult.Id, out var binding))
                return binding.ResolveActiveVerb();
            return null;
        }

        /// <summary>
        /// 清除挂起的 target hint。由 GetRangedAttackAction Postfix 调用。
        /// </summary>
        internal void ClearPendingRangedTargetHint()
        {
            pendingRangedTargetResultHint = null;
        }

    }
}
