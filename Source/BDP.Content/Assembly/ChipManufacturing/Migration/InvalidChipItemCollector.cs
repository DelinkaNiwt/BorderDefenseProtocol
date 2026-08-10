using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Core.Trigger;
using RimWorld.Planet;
using Verse;
using VerseThing = Verse.Thing;

namespace BDP.Content.Assembly.ChipManufacturing.Migration
{
    /// <summary>等待判定的持久化芯片实体及其组合记录。</summary>
    public sealed class InvalidChipCandidate
    {
        /// <summary>实体物品。</summary>
        public VerseThing Item { get; set; }

        /// <summary>实体持有的组合记录。</summary>
        public ChipCombinationRecord Record { get; set; }
    }

    /// <summary>从地图、递归容器、世界对象和触发体槽位收集芯片实体。</summary>
    public sealed class InvalidChipItemCollector
    {
        /// <summary>收集当前游戏中全部带组合记录的物品，并按 ThingID 去重。</summary>
        public IReadOnlyList<InvalidChipCandidate> Collect()
        {
            List<VerseThing> things = new List<VerseThing>();
            List<InvalidChipCandidate> candidates = new List<InvalidChipCandidate>();
            HashSet<string> seenThingIds = new HashSet<string>();

            foreach (Map map in Find.Maps)
            {
                things.Clear();
                ThingOwnerUtility.GetAllThingsRecursively(
                    map,
                    ThingRequest.ForGroup(ThingRequestGroup.Everything),
                    things,
                    true,
                    null,
                    true);
                AddThings(things, seenThingIds, candidates);
                AddTriggerSlots(map.mapPawns.AllPawns, seenThingIds, candidates);
            }

            if (Find.WorldObjects != null)
            {
                foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
                {
                    AddHolder(worldObject as IThingHolder, seenThingIds, candidates);
                }
            }

            if (Find.WorldPawns != null)
            {
                AddTriggerSlots(
                    Find.WorldPawns.AllPawnsAliveOrDead,
                    seenThingIds,
                    candidates);
                foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
                {
                    AddHolder(pawn, seenThingIds, candidates);
                }
            }

            return candidates;
        }

        /// <summary>递归读取一个世界容器中的全部物品。</summary>
        private static void AddHolder(
            IThingHolder holder,
            HashSet<string> seenThingIds,
            List<InvalidChipCandidate> candidates)
        {
            if (holder == null)
            {
                return;
            }

            List<VerseThing> things = ThingOwnerUtility.GetAllThingsRecursively(holder, true);
            AddThings(things, seenThingIds, candidates);
        }

        /// <summary>补充触发体专用槽位中的物品；这些引用不假定由普通 ThingOwner 暴露。</summary>
        private static void AddTriggerSlots(
            IEnumerable<Pawn> pawns,
            HashSet<string> seenThingIds,
            List<InvalidChipCandidate> candidates)
        {
            if (pawns == null)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                ITriggerLoadoutReader reader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
                if (reader == null)
                {
                    continue;
                }

                foreach (ITriggerSlotState slot in reader.GetAllSlots())
                {
                    if (slot != null && !slot.IsBindingMirror && slot.LoadedChip != null)
                    {
                        AddThing(slot.LoadedChip, seenThingIds, candidates);
                    }
                }
            }
        }

        /// <summary>把一组物品加入候选集合。</summary>
        private static void AddThings(
            IEnumerable<VerseThing> things,
            HashSet<string> seenThingIds,
            List<InvalidChipCandidate> candidates)
        {
            foreach (VerseThing thing in things)
            {
                AddThing(thing, seenThingIds, candidates);
            }
        }

        /// <summary>识别单个物品的组合记录并按稳定身份去重。</summary>
        private static void AddThing(
            VerseThing thing,
            HashSet<string> seenThingIds,
            List<InvalidChipCandidate> candidates)
        {
            if (thing == null || thing.Destroyed)
            {
                return;
            }

            string identity = !thing.ThingID.NullOrEmpty()
                ? thing.ThingID
                : "runtime:" + thing.GetHashCode();
            if (!seenThingIds.Add(identity))
            {
                return;
            }

            ChipCombinationRecord record = ResolveRecord(thing);
            if (record != null)
            {
                candidates.Add(new InvalidChipCandidate
                {
                    Item = thing,
                    Record = record
                });
            }
        }

        /// <summary>从物品本体或其组件读取 Content 组合记录。</summary>
        private static ChipCombinationRecord ResolveRecord(VerseThing thing)
        {
            IChipCombinationRecordHolder direct = thing as IChipCombinationRecordHolder;
            if (direct != null)
            {
                return direct.CombinationRecord;
            }

            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps == null || thingWithComps.AllComps == null)
            {
                return null;
            }

            foreach (ThingComp comp in thingWithComps.AllComps)
            {
                IChipCombinationRecordHolder holder = comp as IChipCombinationRecordHolder;
                if (holder?.CombinationRecord != null)
                {
                    return holder.CombinationRecord;
                }
            }

            return null;
        }
    }
}
