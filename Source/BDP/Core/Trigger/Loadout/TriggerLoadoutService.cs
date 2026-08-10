using System;
using BDP.Core.Chips;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体芯片装卸服务。
    /// 负责把槽位、绑定关系和正式容器的一次装卸事务收口在一起。
    /// </summary>
    internal static class TriggerLoadoutService
    {
        /// <summary>
        /// 处理装入芯片请求。
        /// </summary>
        public static bool TryLoadChip(TriggerLoadoutContext context, TriggerSide side, int slotIndex, Thing chip)
        {
            TriggerSlotState slot = context.GetSlot(side, slotIndex);
            if (slot == null)
            {
                BdpDiagnostics.Throttled("trigger.load.reject.slot." + side + "." + slotIndex, "Load rejected: target slot does not exist. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            if (chip == null)
            {
                BdpDiagnostics.Throttled("trigger.load.reject.nullchip." + side + "." + slotIndex, "Load rejected: chip is null. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            if (slot.LoadedChip != null)
            {
                BdpDiagnostics.Throttled("trigger.load.reject.occupied." + side + "." + slotIndex, "Load rejected: slot already occupied. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            TriggerSlotState mirrorSlot;
            string rejectCode = null;
            string rejectMessage = null;
            if (context.TriggerService == null
                || !context.TriggerService.TryResolvePairedOccupancyLoad(side, slotIndex, chip, context.GetSlot, out mirrorSlot, out rejectCode, out rejectMessage))
            {
                if (context.TriggerService == null)
                {
                    rejectCode = "missing_trigger_service";
                    rejectMessage = "装卸事务缺少正式 TriggerService";
                }

                BdpDiagnostics.Throttled("trigger.load.reject." + rejectCode + "." + side + "." + slotIndex, "Load rejected: " + rejectMessage + " side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            ChipLoadoutContract constraint = context.GetChipLoadout(chip);
            if (context.EnsureChipInFormalContainer == null
                || !context.EnsureChipInFormalContainer(chip))
            {
                BdpDiagnostics.Throttled(
                    "trigger.load.reject.formal_container." + side + "." + slotIndex,
                    "Load rejected: chip could not enter formal container before slot commit. side="
                    + side
                    + ", index="
                    + slotIndex
                    + ", chip="
                    + SafeThingId(chip),
                    30);
                return false;
            }

            if (constraint != null
                && constraint.SlotOccupancy == ChipSlotOccupancy.PairedHands)
            {
                slot.SetLoadedChip(chip);
                mirrorSlot.SetLoadedChip(chip);
                slot.SetBinding(false, side, slotIndex, mirrorSlot.Side, mirrorSlot.Index);
                mirrorSlot.SetBinding(true, side, slotIndex, side, slotIndex);
                context.NotifySlotLoadoutChanged(mirrorSlot.Side, mirrorSlot.Index, chip);
            }
            else
            {
                slot.ClearBinding();
                slot.SetLoadedChip(chip);
            }

            context.SyncContainerFromSlotTruth();
            context.NotifySlotLoadoutChanged(side, slotIndex, chip);
            return true;
        }

        /// <summary>
        /// 处理卸下芯片请求。
        /// </summary>
        public static bool TryUnloadChip(TriggerLoadoutContext context, TriggerSide side, int slotIndex)
        {
            TriggerSlotState slot = context.NormalizeDirectControlSlot(context.GetSlot(side, slotIndex));
            if (slot == null)
            {
                BdpDiagnostics.Throttled("trigger.unload.reject.slot." + side + "." + slotIndex, "Unload rejected: target slot does not exist. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            if (slot.IsActive)
            {
                BdpDiagnostics.Throttled("trigger.unload.reject.active." + side + "." + slotIndex, "Unload rejected: slot is still active. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            if (slot.LoadedChip == null)
            {
                BdpDiagnostics.Throttled("trigger.unload.reject.empty." + side + "." + slotIndex, "Unload rejected: slot is empty. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            Thing removedChip = slot.LoadedChip;
            TriggerSlotState mirrorSlot = context.GetBindingPartnerSlot(slot);
            if ((mirrorSlot != null && mirrorSlot.IsActive) || slot.IsActive)
            {
                BdpDiagnostics.Throttled("trigger.unload.reject.binding_active." + side + "." + slotIndex, "Unload rejected: binding pair is still active. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            slot.SetLoadedChip(null);
            slot.ClearBinding();
            if (mirrorSlot != null)
            {
                mirrorSlot.SetLoadedChip(null);
                mirrorSlot.SetActive(false);
                mirrorSlot.ClearBinding();
                context.NotifySlotLoadoutChanged(mirrorSlot.Side, mirrorSlot.Index, removedChip);
            }

            context.SyncContainerFromSlotTruth();
            context.NotifySlotLoadoutChanged(side, slotIndex, removedChip);
            return true;
        }

        /// <summary>
        /// 销毁指定槽位中与目标 ThingID 匹配的已装载芯片。
        /// 它用于一次性来源芯片消费，不对外暴露槽位内部细节。
        /// </summary>
        public static bool TryDestroyLoadedChip(
            TriggerLoadoutContext context,
            TriggerSide side,
            int slotIndex,
            string expectedThingId)
        {
            TriggerSlotState slot = context.NormalizeDirectControlSlot(context.GetSlot(side, slotIndex));
            if (slot == null)
            {
                BdpDiagnostics.Throttled("trigger.destroy.reject.slot." + side + "." + slotIndex, "Destroy rejected: target slot does not exist. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            Thing loadedChip = slot.LoadedChip;
            if (loadedChip == null)
            {
                BdpDiagnostics.Throttled("trigger.destroy.reject.empty." + side + "." + slotIndex, "Destroy rejected: slot is empty. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            if (string.IsNullOrWhiteSpace(expectedThingId) || !string.Equals(loadedChip.ThingID, expectedThingId, StringComparison.Ordinal))
            {
                BdpDiagnostics.Throttled("trigger.destroy.reject.mismatch." + side + "." + slotIndex, "Destroy rejected: slot chip does not match expected thing id. side=" + side + ", index=" + slotIndex, 30);
                return false;
            }

            TriggerSlotState mirrorSlot = context.GetBindingPartnerSlot(slot);
            context.SetSwitchContext?.Invoke(slot.Side, null);
            if (mirrorSlot != null)
            {
                context.SetSwitchContext?.Invoke(mirrorSlot.Side, null);
            }

            if (slot.IsActive)
            {
                slot.SetActive(false);
            }

            if (mirrorSlot != null && mirrorSlot.IsActive)
            {
                mirrorSlot.SetActive(false);
            }

            slot.SetLoadedChip(null);
            slot.ClearBinding();
            if (mirrorSlot != null)
            {
                mirrorSlot.SetLoadedChip(null);
                mirrorSlot.SetActive(false);
                mirrorSlot.ClearBinding();
                context.NotifySlotLoadoutChanged(mirrorSlot.Side, mirrorSlot.Index, loadedChip);
            }

            context.SyncContainerFromSlotTruth();
            if (!loadedChip.Destroyed)
            {
                loadedChip.Destroy(DestroyMode.Vanish);
            }

            context.NotifySlotLoadoutChanged(side, slotIndex, loadedChip);
            return true;
        }

        /// <summary>
        /// 安全读取 ThingID。
        /// </summary>
        private static string SafeThingId(Thing thing)
        {
            return thing != null && !string.IsNullOrWhiteSpace(thing.ThingID)
                ? thing.ThingID
                : "null";
        }

    }

    /// <summary>
    /// 触发体装卸事务所需的最小上下文。
    /// 只聚合服务所需的委托，不额外引入新的 owner。
    /// </summary>
    internal sealed class TriggerLoadoutContext
    {
        /// <summary>
        /// Trigger 正式服务入口。
        /// </summary>
        public TriggerService TriggerService;

        /// <summary>
        /// 读取目标槽位。
        /// </summary>
        public Func<TriggerSide, int, TriggerSlotState> GetSlot;

        /// <summary>
        /// 把镜像副槽位归一到主控槽位。
        /// </summary>
        public Func<TriggerSlotState, TriggerSlotState> NormalizeDirectControlSlot;

        /// <summary>
        /// 读取绑定对侧槽位。
        /// </summary>
        public Func<TriggerSlotState, TriggerSlotState> GetBindingPartnerSlot;

        /// <summary>
        /// 读取芯片装载声明结果。
        /// </summary>
        public Func<Thing, ChipLoadoutContract> GetChipLoadout;

        /// <summary>
        /// 按当前 slot 真值同步正式容器。
        /// </summary>
        public Action SyncContainerFromSlotTruth;

        /// <summary>
        /// 在写入槽位真值前，先确保芯片能够进入正式容器。
        /// 返回 false 表示本次装载事务必须整体拒绝，避免半提交。
        /// </summary>
        public Func<Thing, bool> EnsureChipInFormalContainer;

        /// <summary>
        /// 广播装载状态变化。
        /// </summary>
        public Action<TriggerSide, int, Thing> NotifySlotLoadoutChanged;

        /// <summary>
        /// 清空指定侧的切换上下文。
        /// 一次性销毁命令会用它清理残留切换态。
        /// </summary>
        public Action<TriggerSide, SwitchContext> SetSwitchContext;

    }
}
