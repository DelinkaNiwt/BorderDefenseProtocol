using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Requirements;
using BDP.Core.CombatBody;
using BDP.Core.Trigger.Runtime;
using BDP.Core.VerbHosting;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体内部上下文与辅助构建片段。
    /// 负责拼装切换、绑定、禁用同步和装卸事务上下文。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 补齐内部协调器和辅助对象。
        /// </summary>
        private void EnsureInternalState()
        {
            if (disableSyncCache == null)
            {
                disableSyncCache = new TriggerDisableSyncCache();
            }

            if (verbHostManager == null)
            {
                verbHostManager = new TriggerBodyVerbHostManager(this);
            }

            if (runtimeCoordinator == null)
            {
                runtimeCoordinator = new TriggerRuntimeCoordinator(this);
            }
        }

        /// <summary>
        /// 确保芯片容器已经建立。
        /// container 只是从属持有设施，不承载芯片装载真值。
        /// </summary>
        private void EnsureChipContainer()
        {
            if (chipContainer == null)
            {
                chipContainer = new ThingOwner<Thing>(this);
            }
        }

        /// <summary>
        /// 确保三组槽位列表已经建立。
        /// </summary>
        private void EnsureSlots()
        {
            if (mainSlots == null)
            {
                mainSlots = BuildSlots(TriggerSide.Main, Props.mainSlotCount);
            }

            if (subSlots == null)
            {
                subSlots = BuildSlots(TriggerSide.Sub, Props.subSlotCount);
            }

            if (specialSlots == null)
            {
                specialSlots = BuildSlots(TriggerSide.Special, Props.specialSlotCount);
            }
        }

        /// <summary>
        /// 按侧别创建一组空槽位状态。
        /// </summary>
        private List<TriggerSlotState> BuildSlots(TriggerSide side, int count)
        {
            List<TriggerSlotState> slots = new List<TriggerSlotState>();
            for (int i = 0; i < count; i++)
            {
                slots.Add(new TriggerSlotState(side, i));
            }

            return slots;
        }

        /// <summary>
        /// 直接取得某一侧的原始槽位列表。
        /// </summary>
        private List<TriggerSlotState> GetRawSlots(TriggerSide side)
        {
            EnsureSlots();

            switch (side)
            {
                case TriggerSide.Main:
                    return mainSlots;
                case TriggerSide.Sub:
                    return subSlots;
                default:
                    return specialSlots;
            }
        }

        /// <summary>
        /// 读取某一侧指定索引的槽位，不存在时返回空。
        /// </summary>
        private TriggerSlotState GetSlot(TriggerSide side, int index)
        {
            List<TriggerSlotState> slots = GetRawSlots(side);
            if (index < 0 || index >= slots.Count)
            {
                return null;
            }

            return slots[index];
        }

        /// <summary>
        /// 读取某一侧保存中的切换上下文。
        /// </summary>
        private SwitchContext GetSwitchContext(TriggerSide side)
        {
            switch (side)
            {
                case TriggerSide.Main:
                    return mainSwitchContext;
                case TriggerSide.Sub:
                    return subSwitchContext;
                default:
                    return specialSwitchContext;
            }
        }

        /// <summary>
        /// 读取某一侧当前生效中的切换上下文。
        /// </summary>
        private SwitchContext GetActiveSwitchContext(TriggerSide side)
        {
            return TriggerSwitchTransitionService.GetActiveSwitchContext(side, GetCurrentTick(), GetSwitchContext);
        }

        /// <summary>
        /// 写回某一侧的切换上下文。
        /// </summary>
        private void SetSwitchContext(TriggerSide side, SwitchContext context)
        {
            switch (side)
            {
                case TriggerSide.Main:
                    mainSwitchContext = context;
                    break;
                case TriggerSide.Sub:
                    subSwitchContext = context;
                    break;
                default:
                    specialSwitchContext = context;
                    break;
            }
        }

        /// <summary>
        /// 构建装卸芯片链所需的宿主上下文。
        /// </summary>
        private TriggerLoadoutContext BuildLoadoutContext()
        {
            return new TriggerLoadoutContext
            {
                TriggerService = triggerService,
                GetSlot = GetSlot,
                NormalizeDirectControlSlot = NormalizeDirectControlSlot,
                GetBindingPartnerSlot = GetBindingPartnerSlot,
                GetChipLoadout = GetChipLoadout,
                SyncContainerFromSlotTruth = RebuildContainerFromSlotTruth,
                EnsureChipInFormalContainer = EnsureChipInContainer,
                NotifySlotLoadoutChanged = NotifySlotLoadoutChanged,
                SetSwitchContext = SetSwitchContext
            };
        }

        /// <summary>
        /// 构建切换链所需的正式上下文。
        /// </summary>
        private TriggerSwitchContext BuildSwitchContext()
        {
            return new TriggerSwitchContext
            {
                GetSlot = GetSlot,
                GetActiveSlot = side => GetActiveSlot(side) as TriggerSlotState,
                GetActiveSlotRaw = GetActiveSlotRaw,
                GetSwitchContext = GetSwitchContext,
                SetSwitchContext = SetSwitchContext,
                FindActivationBlockers = target => triggerService.FindActivationBlockers(
                    target,
                    GetActiveSlotsRaw(),
                    GetSlot),
                HasActivationExclusionConflict = triggerService.HasActivationExclusionConflict,
                IsPendingTargetValid = IsPendingTargetValid,
                EvaluateActivationRequirements = chip =>
                    ChipActivationRequirementService.Instance.Evaluate(OwnerPawn, chip),
                ResolveChipActivationDelayTicks = triggerService.ResolveChipActivationDelayTicks,
                ResolveChipDeactivationDelayTicks = triggerService.ResolveChipDeactivationDelayTicks,
                CurrentTick = GetCurrentTick(),
                NotifySlotActivationCommitted = NotifySlotActivationCommitted,
                NotifySlotDeactivated = NotifySlotDeactivated
            };
        }

        /// <summary>
        /// 读取某个芯片当前适用的正式装载声明结果。
        /// </summary>
        private BDP.Core.Chips.ChipLoadoutContract GetChipLoadout(Thing chip)
        {
            return triggerService.GetChipLoadout(chip);
        }

        /// <summary>
        /// 把某个槽位归一到真正受控的槽位上。
        /// </summary>
        private TriggerSlotState NormalizeDirectControlSlot(TriggerSlotState slot)
        {
            return TriggerSwitchTransitionService.NormalizeDirectControlSlot(slot, GetSlot);
        }

        /// <summary>
        /// 读取与当前槽位互相绑定的另一侧槽位。
        /// </summary>
        private TriggerSlotState GetBindingPartnerSlot(TriggerSlotState slot)
        {
            return TriggerSwitchTransitionService.GetBindingPartnerSlot(slot, GetSlot);
        }

        /// <summary>
        /// 判断这次启停是否应走主副手同步切换。
        /// </summary>
        private bool ShouldUseSynchronizedHandTransition(TriggerSlotState selectedSlot)
        {
            return TriggerSwitchTransitionService.ShouldUseSynchronizedHandTransition(selectedSlot, GetSlot, GetActiveSlotRaw);
        }

        /// <summary>
        /// 判断一项尚未完成的启用目标是否仍可继续。
        /// </summary>
        private bool IsPendingTargetValid(TriggerSlotState targetSlot)
        {
            if (CombatBodySurfaceAccess.ResolveReader(OwnerPawn)?.Phase != CombatBodyPhase.Active
                || !IsPendingTargetSlotValid(targetSlot))
            {
                return false;
            }

            if (!targetSlot.HasBindingPartner)
            {
                return true;
            }

            TriggerSlotState bindingPartner = GetBindingPartnerSlot(targetSlot);
            return bindingPartner != null
                && bindingPartner.LoadedChip == targetSlot.LoadedChip
                && IsPendingTargetSlotValid(bindingPartner);
        }

        /// <summary>
        /// 判断等待目标中的单个实体槽位是否仍可参与正式激活。
        /// 成对目标会分别用本边界检查根槽与镜像槽，避免只启用其中一侧。
        /// </summary>
        private bool IsPendingTargetSlotValid(TriggerSlotState targetSlot)
        {
            return targetSlot != null
                && targetSlot.LoadedChip != null
                && !targetSlot.IsDisabled
                && triggerService.GetChipLoadout(targetSlot.LoadedChip) != null;
        }

        /// <summary>
        /// 为正式投影构建拍下一侧槽位真值快照。
        /// 快照只复制 owner 当前成立的最小业务事实，不把公共 reader 引回 owner 自己。
        /// </summary>
        private List<TriggerSlotState> SnapshotSlotsForProjectionBuild(TriggerSide side)
        {
            List<TriggerSlotState> snapshot = new List<TriggerSlotState>();
            foreach (TriggerSlotState slot in GetRawSlots(side))
            {
                snapshot.Add(CloneSlotForProjectionBuild(slot));
            }

            return snapshot;
        }

        /// <summary>
        /// 克隆单个槽位的最小正式真值。
        /// </summary>
        private static TriggerSlotState CloneSlotForProjectionBuild(TriggerSlotState slot)
        {
            if (slot == null)
            {
                return null;
            }

            TriggerSlotState clone = new TriggerSlotState(slot.Side, slot.Index);
            clone.SetLoadedChip(slot.LoadedChip);
            clone.SetDisabled(slot.IsDisabled, slot.DisabledReason);
            clone.SetActive(slot.IsActive);
            if (slot.HasBindingPartner)
            {
                clone.SetBinding(
                    slot.IsBindingMirror,
                    slot.BindingRootSide,
                    slot.BindingRootIndex,
                    slot.BindingPartnerSide,
                    slot.BindingPartnerIndex);
            }

            // 当前形态属于正式投影输入的一部分。
            // 必须在绑定关系之后写入，确保镜像槽仍保持无独立形态。
            if (!clone.IsBindingMirror)
            {
                clone.SetCurrentModeKey(slot.CurrentModeKey);
            }

            return clone;
        }

        /// <summary>
        /// 克隆单侧切换上下文，避免运行时构建读取到可变原对象。
        /// </summary>
        private static SwitchContext CloneSwitchContextForProjectionBuild(SwitchContext context)
        {
            if (context == null)
            {
                return null;
            }

            return new SwitchContext
            {
                phase = context.phase,
                phaseEndTick = context.phaseEndTick,
                targetSlotIndex = context.targetSlotIndex,
                targetChipThingId = context.targetChipThingId,
                deactivatingSlotIndex = context.deactivatingSlotIndex,
                activationDelayDuration = context.activationDelayDuration,
                deactivationDelayDuration = context.deactivationDelayDuration
            };
        }

        /// <summary>
        /// 读取某一侧当前原始激活槽位。
        /// </summary>
        private TriggerSlotState GetActiveSlotRaw(TriggerSide side)
        {
            foreach (TriggerSlotState slot in GetRawSlots(side))
            {
                if (slot.IsActive)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// 枚举当前所有原始激活槽位。
        /// </summary>
        private IEnumerable<TriggerSlotState> GetActiveSlotsRaw()
        {
            foreach (TriggerSlotState slot in EnumerateRawSlots())
            {
                if (slot.IsActive)
                {
                    yield return slot;
                }
            }
        }

    }
}
