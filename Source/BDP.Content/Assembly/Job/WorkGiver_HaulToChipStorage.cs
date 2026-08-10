using System.Collections.Generic;
using BDP.Core.Chips;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 把地图上的有效芯片搬入芯片仓的工作扫描器。
    /// </summary>
    public class WorkGiver_HaulToChipStorage : WorkGiver_Scanner
    {
        /// <summary>
        /// 芯片仓 Def 名称。
        /// </summary>
        private const string ChipStorageDefName = "BDP_ChipStorage";

        /// <summary>
        /// 工作扫描使用地图物品组。
        /// </summary>
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForGroup(ThingRequestGroup.HaulableEver); }
        }

        /// <summary>
        /// 小人需要接触芯片才能搬运。
        /// </summary>
        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.Touch; }
        }

        /// <summary>
        /// 枚举当前地图上的全部物品，随后由 HasJobOnThing 过滤有效芯片。
        /// </summary>
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                yield break;
            }

            List<Thing> items = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
            for (int i = 0; i < items.Count; i++)
            {
                yield return items[i];
            }
        }

        /// <summary>
        /// 判断指定物品是否能生成搬入芯片仓的工作。
        /// </summary>
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!IsHaulableChipForPawn(pawn, t, forced))
            {
                return false;
            }

            return FindChipStorage(pawn, t, forced) != null;
        }

        /// <summary>
        /// 为指定芯片生成搬运到芯片仓的 Job。
        /// </summary>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Thing storage = FindChipStorage(pawn, t, forced);
            if (storage == null)
            {
                return null;
            }

            JobDef jobDef = AssemblyJobDefs.HaulToChipStorage;
            if (jobDef == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(jobDef, t, storage);
            job.count = 1;
            return job;
        }

        /// <summary>
        /// 寻找最近可用、可到达、可预留的芯片仓。
        /// </summary>
        private static Thing FindChipStorage(Pawn pawn, Thing chip, bool forced)
        {
            if (pawn == null || chip == null || chip.Map == null)
            {
                return null;
            }

            ThingDef chipStorageDef = DefDatabase<ThingDef>.GetNamedSilentFail(ChipStorageDefName);
            if (chipStorageDef == null)
            {
                return null;
            }

            return GenClosest.ClosestThingReachable(
                chip.Position,
                chip.Map,
                ThingRequest.ForDef(chipStorageDef),
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                9999f,
                storage => IsUsableStorageForPawn(pawn, storage, forced));
        }

        /// <summary>
        /// 判断物品是否是当前小人可以搬运的有效芯片。
        /// </summary>
        private static bool IsHaulableChipForPawn(Pawn pawn, Thing thing, bool forced)
        {
            if (pawn == null || thing == null || thing.Destroyed || !thing.Spawned)
            {
                return false;
            }

            if (thing.stackCount != 1)
            {
                return false;
            }

            if (thing.IsForbidden(pawn))
            {
                return false;
            }

            if (!pawn.CanReserveAndReach(thing, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, 1, null, forced))
            {
                return false;
            }

            return IsValidChipDefinition(thing);
        }

        /// <summary>
        /// 判断芯片仓是否可作为当前搬运目标。
        /// </summary>
        private static bool IsUsableStorageForPawn(Pawn pawn, Thing storage, bool forced)
        {
            if (pawn == null || storage == null || storage.Destroyed || storage.IsForbidden(pawn))
            {
                return false;
            }

            if (!pawn.CanReserve(storage, 1, -1, null, forced))
            {
                return false;
            }

            CompChipContainer container = storage.TryGetComp<CompChipContainer>();
            return container != null && container.CanAcceptMore;
        }

        /// <summary>
        /// 判断物品定义是否通过 BDP 芯片校验。
        /// </summary>
        private static bool IsValidChipDefinition(Thing thing)
        {
            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(thing);
            return snapshot != null && snapshot.IsValid;
        }
    }
}
