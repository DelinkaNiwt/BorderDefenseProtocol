using System.Collections.Generic;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Trion;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体完整性与广播辅助片段。
    /// 负责容器一致性修复和若干状态广播。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 按 slot 自身记录的芯片标识恢复读档后的槽位真值。
        /// 恢复过程只允许“slot 指向实际 Thing”，不允许 container 反向猜测槽位语义。
        /// </summary>
        private bool RestoreSlotTruth()
        {
            EnsureChipContainer();
            Dictionary<string, Thing> chipsById = BuildChipLookup();
            bool allResolved = true;

            foreach (TriggerSlotState slot in EnumerateRawSlots())
            {
                Thing chip = slot.LoadedChip;
                if (chip == null && !string.IsNullOrWhiteSpace(slot.LoadedChipThingId))
                {
                    chipsById.TryGetValue(slot.LoadedChipThingId, out chip);
                    slot.RestoreLoadedChipReference(chip);
                    if (chip == null)
                    {
                        allResolved = false;
                        BdpDiagnostics.Once(
                            "trigger.slot_truth_missing_after_load." + sideKey(slot),
                            "读档后槽位真值声明了芯片标识，但未能绑定回实际芯片。side=" + slot.Side + ", index=" + slot.Index + ", chipThingId=" + slot.LoadedChipThingId);
                        continue;
                    }
                }

                if (chip == null)
                {
                    slot.SetActive(false);
                    continue;
                }

                if (chip.Destroyed)
                {
                    BdpDiagnostics.Once("trigger.integrity.destroyed." + slot.Side + "." + slot.Index, "槽位记录的芯片已经销毁，清空引用。side=" + slot.Side + ", index=" + slot.Index);
                    slot.SetLoadedChip(null);
                    slot.SetActive(false);
                    continue;
                }

            }

            NormalizeRestoredChipModes();
            return allResolved;
        }

        /// <summary>
        /// 在槽位芯片引用恢复后正规化当前形态。
        /// 有效保存值保留；空值或失效值回到默认形态；镜像、失活和单形态槽保持为空。
        /// </summary>
        private void NormalizeRestoredChipModes()
        {
            foreach (TriggerSlotState slot in EnumerateRawSlots())
            {
                if (slot == null)
                {
                    continue;
                }

                string discardedModeKey;
                bool changed = TriggerChipModeService.NormalizeRestoredActiveRootMode(
                    slot,
                    slot.LoadedChip,
                    out discardedModeKey);
                if (!changed
                    || !slot.IsActive
                    || slot.IsBindingMirror
                    || string.IsNullOrWhiteSpace(discardedModeKey)
                    || string.IsNullOrWhiteSpace(slot.CurrentModeKey))
                {
                    continue;
                }

                BdpDiagnostics.Once(
                    "trigger.chip_mode_post_load_fallback."
                    + slot.LoadedChipThingId + "." + discardedModeKey,
                    "读档保存的芯片形态已不存在，已回退默认形态。chipThingId="
                    + slot.LoadedChipThingId
                    + ", oldMode=" + discardedModeKey
                    + ", defaultMode=" + slot.CurrentModeKey);
            }
        }

        /// <summary>
        /// 仅按 slot 真值重建芯片容器。
        /// container 是从属存储，不能再反过来定义业务状态。
        /// </summary>
        private void RebuildContainerFromSlotTruth()
        {
            EnsureChipContainer();
            HashSet<Thing> expectedChips = new HashSet<Thing>();
            foreach (TriggerSlotState slot in EnumerateRawSlots())
            {
                if (slot.LoadedChip != null)
                {
                    expectedChips.Add(slot.LoadedChip);
                }
            }

            for (int i = chipContainer.Count - 1; i >= 0; i--)
            {
                Thing chip = chipContainer[i];
                if (chip == null || expectedChips.Contains(chip))
                {
                    continue;
                }

                chipContainer.Remove(chip);
                BdpDiagnostics.Once("trigger.integrity.orphan_container_chip." + chip.ThingID, "正式容器存在未被任何槽位真值声明的孤儿芯片，已移出。chip=" + chip.LabelShortCap);
            }

            foreach (Thing chip in expectedChips)
            {
                if (!EnsureChipInContainer(chip))
                {
                    BdpDiagnostics.Once(
                        "trigger.integrity.rebuild_failed." + chip.ThingID,
                        "按槽位真值回收芯片进入正式容器失败。chip=" + (chip != null ? chip.LabelShortCap : "null"));
                }
            }
        }

        /// <summary>
        /// 把期望中的芯片放回正式容器，必要时先从其它持有者转移。
        /// </summary>
        private bool EnsureChipInContainer(Thing chip)
        {
            if (chip == null)
            {
                return false;
            }

            if (IsActuallyInChipContainer(chip))
            {
                return true;
            }

            if (chip.holdingOwner == chipContainer)
            {
                chip.holdingOwner = null;
            }

            if (chip.Spawned)
            {
                chip.DeSpawn();
            }

            if (chip.holdingOwner != null)
            {
                // Trigger formal container must preserve one chip object per slot-owned identity.
                // If merge is allowed here, equal-def chips can be absorbed into an older stack and
                // the slot will keep pointing at a destroyed object identity.
                return chip.holdingOwner.TryTransferToContainer(
                    chip,
                    chipContainer,
                    canMergeWithExistingStacks: false);
            }

            return chipContainer.TryAdd(chip, canMergeWithExistingStacks: false);
        }

        /// <summary>
        /// 构建当前芯片容器的 ThingID 索引。
        /// </summary>
        private Dictionary<string, Thing> BuildChipLookup()
        {
            Dictionary<string, Thing> chipsById = new Dictionary<string, Thing>();
            if (chipContainer == null)
            {
                return chipsById;
            }

            for (int i = 0; i < chipContainer.Count; i++)
            {
                Thing chip = chipContainer[i];
                if (chip == null || string.IsNullOrWhiteSpace(chip.ThingID))
                {
                    continue;
                }

                chipsById[chip.ThingID] = chip;
            }

            return chipsById;
        }

        private static string sideKey(TriggerSlotState slot)
        {
            return slot != null ? slot.Side + "." + slot.Index : "null";
        }

        /// <summary>
        /// 判断芯片是否真实存在于正式容器列表中。
        /// 不能只信 holdingOwner，因为 RimWorld 的 Contains 仅检查 owner 指针。
        /// </summary>
        private bool IsActuallyInChipContainer(Thing chip)
        {
            if (chip == null || chipContainer == null)
            {
                return false;
            }

            for (int i = 0; i < chipContainer.Count; i++)
            {
                if (ReferenceEquals(chipContainer[i], chip))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 枚举全部原始槽位，不做激活或展示过滤。
        /// </summary>
        private IEnumerable<TriggerSlotState> EnumerateRawSlots()
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
        /// 对外广播槽位装配内容已变化。
        /// </summary>
        private void NotifySlotLoadoutChanged(TriggerSide side, int slotIndex, Thing chip)
        {
            SlotLoadoutChanged?.Invoke(new TriggerSlotStateChangedArgs
            {
                Side = side,
                SlotIndex = slotIndex,
                Chip = chip
            });
        }

        /// <summary>
        /// 对外广播槽位激活已正式提交。
        /// 广播前先发布正式战斗投影，确保表达结果与 Verb 宿主表只观察到正式激活态。
        /// </summary>
        private void NotifySlotActivationCommitted(TriggerSide side, int slotIndex, Thing chip)
        {
            if (!TryCommitSlotActivationTrion(chip))
            {
                TriggerSwitchTransitionService.DeactivateBoundSlotImmediate(GetSlot(side, slotIndex), GetSlot, SetSwitchContext, NotifySlotDeactivated);
                return;
            }

            TriggerSlotState rootSlot = NormalizeDirectControlSlot(GetSlot(side, slotIndex));
            if (!TriggerChipModeService.TryInitializeActiveRootMode(rootSlot, chip))
            {
                TriggerSwitchTransitionService.DeactivateBoundSlotImmediate(
                    rootSlot,
                    GetSlot,
                    SetSwitchContext,
                    NotifySlotDeactivated);
                return;
            }

            PublishCombatProjection(ProjectionDirtyReason.SlotActivationCommitted);
            SlotActivationCommitted?.Invoke(new TriggerSlotStateChangedArgs
            {
                Side = side,
                SlotIndex = slotIndex,
                Chip = chip
            });
        }

        /// <summary>
        /// 对外广播槽位已经停用。
        /// 广播前先发布正式战斗投影，确保表达结果与 Verb 宿主表只观察到正式停用态。
        /// </summary>
        private void NotifySlotDeactivated(TriggerSide side, int slotIndex, Thing chip)
        {
            PublishCombatProjection(ProjectionDirtyReason.SlotDeactivated);
            SlotDeactivated?.Invoke(new TriggerSlotStateChangedArgs
            {
                Side = side,
                SlotIndex = slotIndex,
                Chip = chip
            });
        }

        /// <summary>
        /// 对外广播槽位禁用原因已变化。
        /// </summary>
        private void NotifySlotDisableStateChanged(TriggerSide side, int slotIndex, Thing chip, TriggerDisableReason reason)
        {
            SlotDisableStateChanged?.Invoke(new TriggerSlotStateChangedArgs
            {
                Side = side,
                SlotIndex = slotIndex,
                Chip = chip,
                DisabledReason = reason
            });
        }

        /// <summary>
        /// 为刚刚正式提交激活的槽位结算一次性 Trion 成本。
        /// 若支付失败，调用方必须立刻撤销这次激活提交。
        /// </summary>
        private bool TryCommitSlotActivationTrion(Thing chip)
        {
            return runtimeServices.TriggerTrionBindingService.TryCommitSlotActivation(
                OwnerPawn,
                chip,
                triggerService);
        }

        /// <summary>
        /// 应用或解除来自 CombatBody 的统一禁用覆盖。
        /// 这条链复用正式禁用真值，并立即落地到槽位、切换上下文和投影发布。
        /// </summary>
        internal bool SetCombatBodyUnavailableDisabled(bool disabled)
        {
            if (combatBodyUnavailableDisable == disabled)
            {
                return false;
            }

            combatBodyUnavailableDisable = disabled;
            bool changed = ForceSyncDisabledStateFromOwnerPawn();
            if (!changed)
            {
                return false;
            }

            MarkCombatProjectionDirty(ProjectionDirtyReason.DisableStateChanged);
            return runtimeCoordinator == null || runtimeCoordinator.RebuildAndPublish();
        }
    }
}
