using System.Collections.Generic;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 基于原版 Facility 连接的装配台设施读取器。
    /// 它只读取 CompAffectedByFacilities 已建立的连接，不自行扫描地图。
    /// </summary>
    internal sealed class DefaultAssemblerFacilityProvider : IAssemblerFacilityProvider
    {
        /// <summary>
        /// 当前装配台。
        /// </summary>
        private readonly Building_TriggerAssembler assembler;

        /// <summary>
        /// 构造默认设施读取器。
        /// </summary>
        internal DefaultAssemblerFacilityProvider(Building_TriggerAssembler assembler)
        {
            this.assembler = assembler;
        }

        /// <summary>
        /// 读取所有有效连接容器中的芯片。
        /// </summary>
        public IReadOnlyList<Thing> GetAvailableChips()
        {
            List<Thing> chips = new List<Thing>();
            foreach (CompChipContainer storage in EnumerateActiveStorageComps())
            {
                IReadOnlyList<Thing> storageChips = storage.GetAvailableChips();
                for (int i = 0; i < storageChips.Count; i++)
                {
                    chips.Add(storageChips[i]);
                }
            }

            return chips;
        }

        /// <summary>
        /// 从持有该芯片的有效连接容器中取出芯片。
        /// </summary>
        public bool TryTakeChip(Thing chip)
        {
            if (chip == null)
            {
                return false;
            }

            foreach (CompChipContainer storage in EnumerateActiveStorageComps())
            {
                if (storage.ContainsChip(chip))
                {
                    return storage.TryTakeChip(chip);
                }
            }

            return false;
        }

        /// <summary>
        /// 将芯片回存到第一个有空间的有效连接容器。
        /// </summary>
        public bool TryStoreChip(Thing chip)
        {
            if (chip == null)
            {
                return false;
            }

            foreach (CompChipContainer storage in EnumerateActiveStorageComps())
            {
                if (storage.CanAcceptMore && storage.TryAcceptChip(chip))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 把无法回存的芯片落在装配台附近。
        /// </summary>
        public void DropChipNearAssembler(Thing chip)
        {
            if (chip == null || chip.Destroyed || chip.Spawned)
            {
                return;
            }

            Map map = assembler != null ? assembler.Map : null;
            if (map == null)
            {
                BdpDiagnostics.Throttled("assembly.drop.missing_map", "装配台芯片落地失败：缺少地图。chip=" + SafeThingLabel(chip), 60);
                return;
            }

            if (!GenPlace.TryPlaceThing(chip, assembler.Position, map, ThingPlaceMode.Near))
            {
                BdpDiagnostics.Throttled("assembly.drop.failed." + SafeThingId(chip), "装配台芯片落地失败。chip=" + SafeThingLabel(chip), 60);
            }
        }

        /// <summary>
        /// 判断任一有效连接容器是否仍有空间。
        /// </summary>
        public bool HasStorageSpace()
        {
            foreach (CompChipContainer storage in EnumerateActiveStorageComps())
            {
                if (storage.CanAcceptMore)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 枚举原版设施系统认定为有效且通电的芯片仓内部容器组件。
        /// </summary>
        private IEnumerable<CompChipContainer> EnumerateActiveStorageComps()
        {
            CompAffectedByFacilities affected = assembler != null
                ? assembler.TryGetComp<CompAffectedByFacilities>()
                : null;
            if (affected == null)
            {
                yield break;
            }

            List<Thing> linkedFacilities = affected.LinkedFacilitiesListForReading;
            if (linkedFacilities == null)
            {
                yield break;
            }

            for (int i = 0; i < linkedFacilities.Count; i++)
            {
                Thing facility = linkedFacilities[i];
                if (facility == null || facility.Destroyed)
                {
                    continue;
                }

                CompFacility facilityComp = facility.TryGetComp<CompFacility>();
                if (facilityComp == null || !facilityComp.CanBeActive)
                {
                    continue;
                }

                CompChipContainer storage = facility.TryGetComp<CompChipContainer>();
                if (storage != null)
                {
                    yield return storage;
                }
            }
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

        /// <summary>
        /// 安全读取芯片显示名。
        /// </summary>
        private static string SafeThingLabel(Thing thing)
        {
            return thing != null ? thing.LabelShortCap : "null";
        }
    }
}
