using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>从物品真实生物编码重建的非持久化全局一人一件索引。</summary>
    public static class BlackTriggerBindingIndex
    {
        private static readonly Dictionary<string, CompBlackTriggerBiocodable> byPawn = new Dictionary<string, CompBlackTriggerBiocodable>();

        /// <summary>从地图物品和全部人物携带物中重建索引。</summary>
        public static void Rebuild()
        {
            byPawn.Clear();
            HashSet<Thing> things = new HashSet<Thing>();
            HashSet<Pawn> pawns = new HashSet<Pawn>();
            if (Verse.Find.Maps != null)
            {
                for (int mapIndex = 0; mapIndex < Verse.Find.Maps.Count; mapIndex++)
                {
                    things.UnionWith(Verse.Find.Maps[mapIndex].listerThings.AllThings);
                    pawns.UnionWith(Verse.Find.Maps[mapIndex].mapPawns.AllPawns);
                }
            }
            pawns.UnionWith(PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead);
            pawns.UnionWith(PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead);
            foreach (Pawn pawn in pawns)
            {
                if (pawn?.EquippedWornOrInventoryThings != null) things.UnionWith(pawn.EquippedWornOrInventoryThings);
            }
            foreach (Thing thing in things)
            {
                CompBlackTriggerBiocodable comp = thing?.TryGetComp<CompBlackTriggerBiocodable>();
                if (comp != null && comp.Biocoded && comp.CodedPawn != null) Register(comp.CodedPawn, comp);
            }
        }

        /// <summary>登记一件已经成功编码的黑触发器。</summary>
        public static void Register(Pawn pawn, CompBlackTriggerBiocodable comp)
        {
            if (pawn == null || comp == null) return;
            byPawn[pawn.GetUniqueLoadID()] = comp;
        }

        /// <summary>清理由指定组件占用的索引。</summary>
        public static void Unregister(CompBlackTriggerBiocodable comp)
        {
            if (comp == null) return;
            List<string> removals = new List<string>();
            foreach (KeyValuePair<string, CompBlackTriggerBiocodable> pair in byPawn) if (pair.Value == comp) removals.Add(pair.Key);
            for (int index = 0; index < removals.Count; index++) byPawn.Remove(removals[index]);
        }

        /// <summary>取得人物当前绑定的黑触发器组件。</summary>
        public static CompBlackTriggerBiocodable Find(Pawn pawn)
        {
            if (pawn == null) return null;
            CompBlackTriggerBiocodable comp;
            if (!byPawn.TryGetValue(pawn.GetUniqueLoadID(), out comp)) return null;
            if (comp.parent == null || comp.parent.Destroyed || !comp.Biocoded || comp.CodedPawn != pawn)
            {
                byPawn.Remove(pawn.GetUniqueLoadID());
                return null;
            }
            return comp;
        }

        /// <summary>判断人物是否已经绑定另一件黑触发器。</summary>
        public static bool HasOther(Pawn pawn, Thing except)
        {
            CompBlackTriggerBiocodable comp = Find(pawn);
            return comp != null && comp.parent != except;
        }
    }
}
