using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Trigger;
using BDP.Core.Trion;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配 Trion 预览服务。
    /// 它只做 UI 估算，不写入 Trion 正式账本。
    /// </summary>
    internal sealed class TriggerAssemblyPreviewService
    {
        /// <summary>
        /// 构建当前装配预览快照。
        /// </summary>
        internal TriggerAssemblyPreviewSnapshot BuildSnapshot(
            Pawn pawn,
            ITriggerLoadoutReader reader,
            bool hasPreviewSlot,
            TriggerSide previewSide,
            int previewIndex,
            Thing previewChip)
        {
            ITrionReader trionReader = TrionSurfaceAccess.ResolveReader(pawn);
            float loadedReserved = CalculateLoadedCapacity(reader);
            float previewReserved = loadedReserved;

            if (hasPreviewSlot && previewChip != null)
            {
                Thing oldChip = FindSlot(reader, previewSide, previewIndex)?.LoadedChip;
                if (oldChip != null && !ReferenceEquals(oldChip, previewChip))
                {
                    previewReserved -= ResolveCapacityCost(oldChip);
                }

                previewReserved += ResolveCapacityCost(previewChip);
            }

            return new TriggerAssemblyPreviewSnapshot
            {
                Cur = trionReader != null ? trionReader.Cur : 0f,
                Max = trionReader != null ? trionReader.Max : 0f,
                Allocated = trionReader != null ? trionReader.Allocated : 0f,
                Reserved = loadedReserved,
                Available = trionReader != null ? trionReader.Available : 0f,
                TotalDrainPerSecond = trionReader != null ? trionReader.TotalDrainPerSecond : 0f,
                PreviewReserved = previewReserved,
                ReservedDelta = previewReserved - loadedReserved
            };
        }

        /// <summary>
        /// 统计当前已装芯片的容量占用，双持镜像只按一个芯片对象计算一次。
        /// </summary>
        private static float CalculateLoadedCapacity(ITriggerLoadoutReader reader)
        {
            if (reader == null)
            {
                return 0f;
            }

            float total = 0f;
            HashSet<Thing> seenChips = new HashSet<Thing>();
            foreach (ITriggerSlotState slot in reader.GetAllSlots())
            {
                if (slot == null || slot.LoadedChip == null || seenChips.Contains(slot.LoadedChip))
                {
                    continue;
                }

                seenChips.Add(slot.LoadedChip);
                total += ResolveCapacityCost(slot.LoadedChip);
            }

            return total;
        }

        /// <summary>
        /// 查找指定槽位。
        /// </summary>
        private static ITriggerSlotState FindSlot(ITriggerLoadoutReader reader, TriggerSide side, int index)
        {
            if (reader == null)
            {
                return null;
            }

            foreach (ITriggerSlotState slot in reader.GetAllSlots())
            {
                if (slot != null && slot.Side == side && slot.Index == index)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// 读取芯片容量占用。
        /// </summary>
        private static float ResolveCapacityCost(Thing chip)
        {
            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(chip);
            return snapshot != null && snapshot.IsValid ? snapshot.CapacityCost : 0f;
        }
    }

    /// <summary>
    /// Trion 装配预览快照。
    /// </summary>
    internal sealed class TriggerAssemblyPreviewSnapshot
    {
        /// <summary>
        /// 当前 Trion。
        /// </summary>
        internal float Cur;

        /// <summary>
        /// 最大 Trion。
        /// </summary>
        internal float Max;

        /// <summary>
        /// 已正式锁定量。
        /// </summary>
        internal float Allocated;

        /// <summary>
        /// 当前已装芯片估算占用。
        /// </summary>
        internal float Reserved;

        /// <summary>
        /// 当前可用量。
        /// </summary>
        internal float Available;

        /// <summary>
        /// 当前持续消耗。
        /// </summary>
        internal float TotalDrainPerSecond;

        /// <summary>
        /// 预览操作后的估算占用。
        /// </summary>
        internal float PreviewReserved;

        /// <summary>
        /// 预览占用变化。
        /// </summary>
        internal float ReservedDelta;
    }
}
