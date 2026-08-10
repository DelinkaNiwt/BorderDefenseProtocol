using BDP.Core.Trigger;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配事务。
    /// 它负责把储物取放与 Trigger 正式装卸命令串成一次可回退操作。
    /// </summary>
    internal sealed class TriggerAssemblyTransaction
    {
        /// <summary>
        /// Trigger 正式装载读取口。
        /// </summary>
        private readonly ITriggerLoadoutReader reader;

        /// <summary>
        /// Trigger 正式装载命令口。
        /// </summary>
        private readonly ITriggerLoadoutCommands commands;

        /// <summary>
        /// 装配台连接设施读取口。
        /// </summary>
        private readonly IAssemblerFacilityProvider provider;

        /// <summary>
        /// 构造触发器装配事务。
        /// </summary>
        internal TriggerAssemblyTransaction(
            ITriggerLoadoutReader reader,
            ITriggerLoadoutCommands commands,
            IAssemblerFacilityProvider provider)
        {
            this.reader = reader;
            this.commands = commands;
            this.provider = provider;
        }

        /// <summary>
        /// 从连接容器取出芯片并装入空槽位。
        /// </summary>
        internal TriggerAssemblyOperationResult TryLoadFromStorage(TriggerSide side, int slotIndex, Thing chip)
        {
            if (!HasRequiredServices())
            {
                return TriggerAssemblyOperationResult.Fail("missing_service", "BDP_Message_Assembly_MissingService".Translate());
            }

            TriggerAssemblyOperationResult controlResult = RejectIfPlayerNonConfigurable();
            if (controlResult != null)
            {
                return controlResult;
            }

            ITriggerSlotState targetSlot = FindSlot(side, slotIndex);
            if (targetSlot == null)
            {
                return TriggerAssemblyOperationResult.Fail("target_slot_missing", "BDP_Message_Assembly_TargetSlotMissing".Translate());
            }

            if (targetSlot.IsBindingMirror)
            {
                return TriggerAssemblyOperationResult.Fail("target_slot_mirror", "BDP_Message_Assembly_TargetSlotMirror".Translate());
            }

            if (targetSlot.LoadedChip != null)
            {
                return TriggerAssemblyOperationResult.Fail("target_slot_occupied", "BDP_Message_Assembly_TargetSlotOccupied".Translate());
            }

            if (chip == null)
            {
                return TriggerAssemblyOperationResult.Fail("chip_missing", "BDP_Message_Assembly_ChipMissing".Translate());
            }

            if (!provider.TryTakeChip(chip))
            {
                return TriggerAssemblyOperationResult.Fail("chip_take_failed", "BDP_Message_Assembly_ChipTakeFailed".Translate());
            }

            if (commands.TryLoadChip(side, slotIndex, chip))
            {
                return TriggerAssemblyOperationResult.Ok("loaded", "BDP_Message_Assembly_Loaded".Translate());
            }

            if (!provider.TryStoreChip(chip))
            {
                provider.DropChipNearAssembler(chip);
            }

            return TriggerAssemblyOperationResult.Fail("load_failed", "BDP_Message_Assembly_LoadFailed".Translate());
        }

        /// <summary>
        /// 从槽位卸下芯片并回存到连接容器。
        /// </summary>
        internal TriggerAssemblyOperationResult TryUnloadToStorage(TriggerSide side, int slotIndex)
        {
            if (!HasRequiredServices())
            {
                return TriggerAssemblyOperationResult.Fail("missing_service", "BDP_Message_Assembly_MissingService".Translate());
            }

            TriggerAssemblyOperationResult controlResult = RejectIfPlayerNonConfigurable();
            if (controlResult != null)
            {
                return controlResult;
            }

            ITriggerSlotState slot = FindSlot(side, slotIndex);
            if (slot == null)
            {
                return TriggerAssemblyOperationResult.Fail("source_slot_missing", "BDP_Message_Assembly_SourceSlotMissing".Translate());
            }

            if (slot.IsBindingMirror)
            {
                return TriggerAssemblyOperationResult.Fail("source_slot_mirror", "BDP_Message_Assembly_SourceSlotMirror".Translate());
            }

            Thing loadedChip = slot.LoadedChip;
            if (loadedChip == null)
            {
                return TriggerAssemblyOperationResult.Fail("source_slot_empty", "BDP_Message_Assembly_SourceSlotEmpty".Translate());
            }

            if (!commands.TryUnloadChip(side, slotIndex))
            {
                return TriggerAssemblyOperationResult.Fail("unload_failed", "BDP_Message_Assembly_UnloadFailed".Translate());
            }

            StoreOrDrop(loadedChip);
            return TriggerAssemblyOperationResult.Ok("unloaded", "BDP_Message_Assembly_Unloaded".Translate());
        }

        /// <summary>
        /// 将来源槽位芯片移动到目标槽位，或与目标槽位芯片交换。
        /// </summary>
        internal TriggerAssemblyOperationResult TryMoveOrSwapSlot(
            TriggerSide sourceSide,
            int sourceIndex,
            TriggerSide targetSide,
            int targetIndex)
        {
            if (!HasRequiredServices())
            {
                return TriggerAssemblyOperationResult.Fail("missing_service", "BDP_Message_Assembly_MissingService".Translate());
            }

            TriggerAssemblyOperationResult controlResult = RejectIfPlayerNonConfigurable();
            if (controlResult != null)
            {
                return controlResult;
            }

            if (sourceSide == targetSide && sourceIndex == targetIndex)
            {
                return TriggerAssemblyOperationResult.Ok("same_slot", "BDP_Message_Assembly_SameSlot".Translate());
            }

            ITriggerSlotState sourceSlot = FindSlot(sourceSide, sourceIndex);
            ITriggerSlotState targetSlot = FindSlot(targetSide, targetIndex);
            if (sourceSlot == null || targetSlot == null)
            {
                return TriggerAssemblyOperationResult.Fail("slot_missing", "BDP_Message_Assembly_SlotMissing".Translate());
            }

            if (sourceSlot.IsBindingMirror || targetSlot.IsBindingMirror)
            {
                return TriggerAssemblyOperationResult.Fail("slot_mirror", "BDP_Message_Assembly_SlotMirror".Translate());
            }

            if (sourceSlot.IsActive || targetSlot.IsActive)
            {
                return TriggerAssemblyOperationResult.Fail("slot_active", "BDP_Message_Assembly_SlotActive".Translate());
            }

            Thing sourceChip = sourceSlot.LoadedChip;
            Thing targetChip = targetSlot.LoadedChip;
            if (sourceChip == null)
            {
                return TriggerAssemblyOperationResult.Fail("source_slot_empty", "BDP_Message_Assembly_SourceSlotEmpty".Translate());
            }

            if (targetChip == null)
            {
                return MoveToEmptySlot(sourceSide, sourceIndex, targetSide, targetIndex, sourceChip);
            }

            return SwapSlots(sourceSide, sourceIndex, targetSide, targetIndex, sourceChip, targetChip);
        }

        /// <summary>
        /// 从连接容器取出新芯片并替换目标槽位旧芯片。
        /// </summary>
        internal TriggerAssemblyOperationResult TryReplaceFromStorage(TriggerSide targetSide, int targetIndex, Thing newChip)
        {
            if (!HasRequiredServices())
            {
                return TriggerAssemblyOperationResult.Fail("missing_service", "BDP_Message_Assembly_MissingService".Translate());
            }

            TriggerAssemblyOperationResult controlResult = RejectIfPlayerNonConfigurable();
            if (controlResult != null)
            {
                return controlResult;
            }

            ITriggerSlotState targetSlot = FindSlot(targetSide, targetIndex);
            if (targetSlot == null)
            {
                return TriggerAssemblyOperationResult.Fail("target_slot_missing", "BDP_Message_Assembly_TargetSlotMissing".Translate());
            }

            if (targetSlot.IsBindingMirror)
            {
                return TriggerAssemblyOperationResult.Fail("target_slot_mirror", "BDP_Message_Assembly_TargetSlotMirror".Translate());
            }

            Thing oldChip = targetSlot.LoadedChip;
            if (oldChip == null)
            {
                return TryLoadFromStorage(targetSide, targetIndex, newChip);
            }

            if (newChip == null)
            {
                return TriggerAssemblyOperationResult.Fail("chip_missing", "BDP_Message_Assembly_NewChipMissing".Translate());
            }

            if (!provider.TryTakeChip(newChip))
            {
                return TriggerAssemblyOperationResult.Fail("chip_take_failed", "BDP_Message_Assembly_NewChipTakeFailed".Translate());
            }

            if (!commands.TryUnloadChip(targetSide, targetIndex))
            {
                StoreOrDrop(newChip);
                return TriggerAssemblyOperationResult.Fail("old_unload_failed", "BDP_Message_Assembly_OldUnloadFailed".Translate());
            }

            if (commands.TryLoadChip(targetSide, targetIndex, newChip))
            {
                StoreOrDrop(oldChip);
                return TriggerAssemblyOperationResult.Ok("replaced", "BDP_Message_Assembly_Replaced".Translate());
            }

            bool oldRestored = commands.TryLoadChip(targetSide, targetIndex, oldChip);
            StoreOrDrop(newChip);
            if (!oldRestored)
            {
                StoreOrDrop(oldChip);
            }

            return TriggerAssemblyOperationResult.Fail("replace_failed", "BDP_Message_Assembly_ReplaceFailed".Translate());
        }

        /// <summary>
        /// 把来源芯片移动到空目标槽。
        /// </summary>
        private TriggerAssemblyOperationResult MoveToEmptySlot(
            TriggerSide sourceSide,
            int sourceIndex,
            TriggerSide targetSide,
            int targetIndex,
            Thing sourceChip)
        {
            if (!commands.TryUnloadChip(sourceSide, sourceIndex))
            {
                return TriggerAssemblyOperationResult.Fail("source_unload_failed", "BDP_Message_Assembly_SourceUnloadFailed".Translate());
            }

            if (commands.TryLoadChip(targetSide, targetIndex, sourceChip))
            {
                return TriggerAssemblyOperationResult.Ok("moved", "BDP_Message_Assembly_Moved".Translate());
            }

            if (!commands.TryLoadChip(sourceSide, sourceIndex, sourceChip))
            {
                StoreOrDrop(sourceChip);
            }

            return TriggerAssemblyOperationResult.Fail("move_failed", "BDP_Message_Assembly_MoveFailed".Translate());
        }

        /// <summary>
        /// 交换两个已装槽位的芯片。
        /// </summary>
        private TriggerAssemblyOperationResult SwapSlots(
            TriggerSide sourceSide,
            int sourceIndex,
            TriggerSide targetSide,
            int targetIndex,
            Thing sourceChip,
            Thing targetChip)
        {
            if (!commands.TryUnloadChip(sourceSide, sourceIndex))
            {
                return TriggerAssemblyOperationResult.Fail("source_unload_failed", "BDP_Message_Assembly_SourceUnloadFailed".Translate());
            }

            if (!commands.TryUnloadChip(targetSide, targetIndex))
            {
                if (!commands.TryLoadChip(sourceSide, sourceIndex, sourceChip))
                {
                    StoreOrDrop(sourceChip);
                }

                return TriggerAssemblyOperationResult.Fail("target_unload_failed", "BDP_Message_Assembly_TargetUnloadFailed".Translate());
            }

            if (!commands.TryLoadChip(targetSide, targetIndex, sourceChip))
            {
                RestoreOrDrop(sourceSide, sourceIndex, sourceChip);
                RestoreOrDrop(targetSide, targetIndex, targetChip);
                return TriggerAssemblyOperationResult.Fail("swap_target_load_failed", "BDP_Message_Assembly_SwapTargetLoadFailed".Translate());
            }

            if (commands.TryLoadChip(sourceSide, sourceIndex, targetChip))
            {
                return TriggerAssemblyOperationResult.Ok("swapped", "BDP_Message_Assembly_Swapped".Translate());
            }

            if (commands.TryUnloadChip(targetSide, targetIndex))
            {
                RestoreOrDrop(sourceSide, sourceIndex, sourceChip);
            }

            RestoreOrDrop(targetSide, targetIndex, targetChip);
            return TriggerAssemblyOperationResult.Fail("swap_source_load_failed", "BDP_Message_Assembly_SwapSourceLoadFailed".Translate());
        }

        /// <summary>
        /// 尝试把芯片装回指定槽位，失败则回存或落地。
        /// </summary>
        private void RestoreOrDrop(TriggerSide side, int index, Thing chip)
        {
            if (chip == null)
            {
                return;
            }

            if (!commands.TryLoadChip(side, index, chip))
            {
                StoreOrDrop(chip);
            }
        }

        /// <summary>
        /// 尝试把芯片回存到容器，失败时落到装配台附近。
        /// </summary>
        private void StoreOrDrop(Thing chip)
        {
            if (chip == null || chip.Destroyed || chip.Spawned)
            {
                return;
            }

            if (!provider.TryStoreChip(chip))
            {
                provider.DropChipNearAssembler(chip);
            }
        }

        /// <summary>
        /// 读取指定槽位。
        /// </summary>
        private ITriggerSlotState FindSlot(TriggerSide side, int slotIndex)
        {
            if (reader == null)
            {
                return null;
            }

            foreach (ITriggerSlotState slot in reader.GetAllSlots())
            {
                if (slot != null && slot.Side == side && slot.Index == slotIndex)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// 判断事务所需依赖是否齐备。
        /// </summary>
        private bool HasRequiredServices()
        {
            return reader != null && commands != null && provider != null;
        }

        /// <summary>
        /// 在任何会改动玩家装配结果的事务开始前，统一拦截定义固定的触发体。
        /// </summary>
        private TriggerAssemblyOperationResult RejectIfPlayerNonConfigurable()
        {
            if (reader != null && reader.LoadoutControlMode == TriggerLoadoutControlMode.PlayerNonConfigurable)
            {
                return TriggerAssemblyOperationResult.Fail(
                    "player_non_configurable",
                    "BDP_Message_TriggerAssembly_PlayerNonConfigurable".Translate());
            }

            return null;
        }
    }
}
