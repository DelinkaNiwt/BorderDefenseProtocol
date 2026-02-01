using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System;
using Verse;
using Verse.Sound;
using RimWorld;
using Verse.AI.Group;
using RimWorld.QuestGen;

namespace GD3
{
    public class QuestPart_BackHome : QuestPart
    {
        public string inSignal;

        public Pawn pawn;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                List<Pawn> pawns = this.pawn.Map.mapPawns.AllPawns.FindAll(p => p.Faction != null && p.Faction == Faction.OfPlayer);
                pawns.SortBy(p => p.Position.DistanceTo(this.pawn.Position));
                Pawn pawn = pawns[0];

                Map home = Find.Maps.Find(m => m.IsPlayerHome);
                IntVec3 vec = RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 result, home, CellFinder.EdgeRoadChance_Neutral) ? result : home.Center;
                pawn.DeSpawn();
                GenSpawn.Spawn(pawn, vec, home, Rot4.South);
                CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(vec, home));

                SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));

                Effecter effecter = EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map, 1f);
                effecter.Trigger(pawn, pawn, -1);
                effecter.Cleanup();
                effecter = EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map, 1f);
                effecter.Trigger(pawn, pawn, -1);
                effecter.Cleanup();

                this.pawn.Destroy(DestroyMode.Vanish);
                PocketMapUtility.DestroyPocketMap(Find.Maps.First(m => m.Biome.defName == "DryOcean"));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref inSignal, "inSignal");
        }
    }
}