using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体固定芯片预装定义校验器。
    /// 只检查静态定义，不创建Thing、不写入槽位，也不判断玩家控制模式。
    /// </summary>
    internal static class TriggerFixedLoadoutValidator
    {
        /// <summary>
        /// 校验触发体固定装载表，并返回可直接交给RimWorld Def错误系统的消息。
        /// </summary>
        public static IEnumerable<string> ConfigErrors(
            CompProperties_TriggerBody properties,
            ThingDef parentDef)
        {
            if (properties == null || properties.fixedLoadout == null || properties.fixedLoadout.Count == 0)
            {
                yield break;
            }

            HashSet<string> occupiedPhysicalSlots = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < properties.fixedLoadout.Count; i++)
            {
                TriggerFixedLoadoutEntry entry = properties.fixedLoadout[i];
                string context = BuildContext(parentDef, i);
                if (entry == null)
                {
                    yield return context + " requires a non-null fixedLoadout entry.";
                    continue;
                }

                if (!Enum.IsDefined(typeof(TriggerSide), entry.side))
                {
                    yield return context + " requires side Main, Sub, or Special.";
                    continue;
                }

                if (entry.slotNumber < 1)
                {
                    yield return context + " slotNumber must be at least 1.";
                    continue;
                }

                int slotIndex = entry.slotNumber - 1;
                int slotCount = ResolveSlotCount(properties, entry.side);
                if (slotIndex >= slotCount)
                {
                    yield return context + " slotNumber " + entry.slotNumber + " exceeds the " + entry.side + " slot count " + slotCount + ".";
                    continue;
                }

                if (entry.chipDef == null)
                {
                    yield return context + " requires chipDef.";
                    continue;
                }

                ChipDefinitionReadResult chipReadResult = ChipSurfaceAccess.Read(entry.chipDef);
                if (chipReadResult == null
                    || chipReadResult.Validation == null
                    || !chipReadResult.Validation.IsValid
                    || chipReadResult.Contract == null
                    || chipReadResult.Contract.Loadout == null)
                {
                    yield return context + " chipDef " + entry.chipDef.defName + " must be a valid chip definition with Loadout.";
                    continue;
                }

                ChipLoadoutContract loadout = chipReadResult.Contract.Loadout;
                if (!TriggerService.IsSlotOccupancyAllowed(loadout.SlotRegion, loadout.SlotOccupancy, entry.side))
                {
                    yield return context + " chipDef " + entry.chipDef.defName + " is incompatible with side " + entry.side + ".";
                    continue;
                }

                if (loadout.SlotOccupancy == ChipSlotOccupancy.PairedHands
                    && !HasPairedPartnerSlot(properties, entry.side, slotIndex))
                {
                    yield return context + " paired-hands chip requires the opposite-side slot " + entry.slotNumber + ".";
                    continue;
                }

                List<string> physicalSlots = ResolvePhysicalSlotKeys(entry.side, slotIndex, loadout.SlotOccupancy);
                bool duplicate = false;
                for (int physicalIndex = 0; physicalIndex < physicalSlots.Count; physicalIndex++)
                {
                    if (occupiedPhysicalSlots.Contains(physicalSlots[physicalIndex]))
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (duplicate)
                {
                    yield return context + " targets a physical slot already occupied by another fixed-loadout entry.";
                    continue;
                }

                for (int physicalIndex = 0; physicalIndex < physicalSlots.Count; physicalIndex++)
                {
                    occupiedPhysicalSlots.Add(physicalSlots[physicalIndex]);
                }
            }
        }

        /// <summary>
        /// 读取当前槽区的定义槽位数量。
        /// </summary>
        private static int ResolveSlotCount(CompProperties_TriggerBody properties, TriggerSide side)
        {
            switch (side)
            {
                case TriggerSide.Main:
                    return properties.mainSlotCount;
                case TriggerSide.Sub:
                    return properties.subSlotCount;
                default:
                    return properties.specialSlotCount;
            }
        }

        /// <summary>
        /// 计算一条预装声明会占用的全部物理槽位。
        /// </summary>
        private static List<string> ResolvePhysicalSlotKeys(
            TriggerSide side,
            int slotIndex,
            ChipSlotOccupancy occupancy)
        {
            List<string> result = new List<string>
            {
                BuildPhysicalSlotKey(side, slotIndex)
            };

            if (occupancy == ChipSlotOccupancy.PairedHands)
            {
                TriggerSide mirrorSide = side == TriggerSide.Main
                    ? TriggerSide.Sub
                    : TriggerSide.Main;
                result.Add(BuildPhysicalSlotKey(mirrorSide, slotIndex));
            }

            return result;
        }

        /// <summary>
        /// 检查成对主副槽芯片的对侧同号槽位是否真实存在。
        /// </summary>
        private static bool HasPairedPartnerSlot(
            CompProperties_TriggerBody properties,
            TriggerSide side,
            int slotIndex)
        {
            if (side != TriggerSide.Main && side != TriggerSide.Sub)
            {
                return false;
            }

            TriggerSide mirrorSide = side == TriggerSide.Main
                ? TriggerSide.Sub
                : TriggerSide.Main;
            return slotIndex < ResolveSlotCount(properties, mirrorSide);
        }

        /// <summary>
        /// 构建稳定的物理槽位键。
        /// </summary>
        private static string BuildPhysicalSlotKey(TriggerSide side, int slotIndex)
        {
            return side + ":" + slotIndex;
        }

        /// <summary>
        /// 构建包含父Def和作者条目序号的错误上下文。
        /// </summary>
        private static string BuildContext(ThingDef parentDef, int entryIndex)
        {
            return (parentDef != null ? parentDef.defName : "<unknown>")
                + ".fixedLoadout["
                + (entryIndex + 1)
                + "]";
        }
    }
}
