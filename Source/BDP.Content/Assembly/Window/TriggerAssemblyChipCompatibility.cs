using BDP.Core.Chips;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口的芯片与槽位类型兼容性判断。
    /// </summary>
    internal static class TriggerAssemblyChipCompatibility
    {
        /// <summary>
        /// 判断芯片是否可以装入目标槽位侧别。
        /// 主手与副手共享 MainSub 槽位类型，特殊槽位只接受 Special 类型。
        /// </summary>
        internal static bool IsCompatibleWithSlot(Thing chip, TriggerSide targetSide)
        {
            if (chip == null)
            {
                return false;
            }

            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(chip);
            if (snapshot == null || !snapshot.IsValid)
            {
                return false;
            }

            return targetSide == TriggerSide.Special
                ? snapshot.SlotRegion == ChipSlotRegion.Special
                : snapshot.SlotRegion == ChipSlotRegion.MainSub;
        }

        /// <summary>
        /// 判断成对占槽芯片在目标编号是否有空闲的对侧槽位。
        /// </summary>
        internal static bool HasAvailablePairedPartner(
            Thing chip,
            ITriggerSlotState targetSlot,
            ITriggerLoadoutReader reader)
        {
            if (chip == null)
            {
                return false;
            }

            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(chip);
            if (snapshot == null || !snapshot.IsValid)
            {
                return false;
            }

            if (snapshot.SlotOccupancy != ChipSlotOccupancy.PairedHands)
            {
                return true;
            }

            if (reader == null
                || targetSlot == null
                || targetSlot.Index < 0
                || (targetSlot.Side != TriggerSide.Main && targetSlot.Side != TriggerSide.Sub))
            {
                return false;
            }

            TriggerSide partnerSide = targetSlot.Side == TriggerSide.Main
                ? TriggerSide.Sub
                : TriggerSide.Main;
            foreach (ITriggerSlotState slot in reader.GetSlots(partnerSide))
            {
                if (slot != null && slot.Index == targetSlot.Index)
                {
                    return slot.LoadedChip == null
                        || (targetSlot.LoadedChip != null
                            && ReferenceEquals(slot.LoadedChip, targetSlot.LoadedChip));
                }
            }

            return false;
        }
    }
}
