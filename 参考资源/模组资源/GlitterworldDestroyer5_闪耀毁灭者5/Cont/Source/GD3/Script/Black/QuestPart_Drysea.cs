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
    public class QuestPart_Drysea : QuestPart
    {
        public string inSignal;

        public Site site;

        public float point;

        public string title;

        public string desc;

        public Pawn pawn;

        private Map sea;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                sea = PocketMapUtility.GeneratePocketMap(new IntVec3(75, 1, 75), GDDefOf.Drysea, null, site.Map);

                List<IntVec3> vecs = sea.AllCells.ToList();
                for (int i = 0; i < vecs.Count; i++)
                {
                    IntVec3 ivec = vecs[i];
                    sea.roofGrid.SetRoof(ivec, RoofDefOf.RoofRockThin);
                    if (!(ivec.x == 37 && ivec.z >= 37) && ivec.GetTerrain(sea).defName == "SoilRich")
                    {
                        GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.Plant_PeaceLily), ivec, sea, ThingPlaceMode.Direct);
                        GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.Plant_Grass), ivec, sea, ThingPlaceMode.Direct);
                    }
                    else if (ivec.x == 37 && ivec.z >= 37 && ivec.GetTerrain(sea).defName != "Ice")
                    {
                        sea.terrainGrid.SetTerrain(ivec, GDDefOf.FlagstoneMarble);
                    }
                }
                List<Thing> things = sea.listerThings.ThingsInGroup(ThingRequestGroup.Plant);
                for (int i = 0; i < things.Count; i++)
                {
                    Plant plant = (Plant)things[i];
                    int num = (int)((1f - plant.Growth) * plant.def.plant.growDays);
                    plant.Age += num;
                    plant.Growth = 1f;
                }

                Pawn pawn = site.Map.Center.GetFirstPawn(site.Map);
                if (pawn == null)
                {
                    Log.Error("dummy not get valid pawn.");
                    return;
                }

                SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));

                Effecter effecter = EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map, 1f);
                effecter.Trigger(pawn, pawn, -1);
                effecter.Cleanup();
                effecter = EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map, 1f);
                effecter.Trigger(pawn, pawn, -1);
                effecter.Cleanup();
                if (sea.Biome.defName != "DryOcean")
                {
                    sea = Find.Maps.Find(m => m.IsPocketMap && m.Biome.defName == "DryOcean");
                    if (GDSettings.DeveloperMode)
                    {
                        Log.Warning("Regenerated dry sea map.");
                    }
                }
                IntVec3 vec = new IntVec3(37, 0, 73);
                pawn.DeSpawn();
                GenSpawn.Spawn(pawn, vec, sea, Rot4.South);
                CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(vec, sea));

                effecter = EffecterDefOf.Skip_Entry.Spawn(pawn, sea, 1f);
                effecter.Trigger(pawn, pawn, -1);
                effecter.Cleanup();
                effecter = EffecterDefOf.Skip_Entry.Spawn(pawn, sea, 1f);
                effecter.Trigger(pawn, pawn, -1);
                effecter.Cleanup();

                pawn.stances.stunner.StunFor(15, pawn, false, false);
                pawn.Notify_Teleported(true, true);
                CompAbilityEffect_Teleport.SendSkipUsedSignal(pawn.Position, pawn);

                SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(vec, sea, false));

                Pawn apocriton = this.pawn;
                CompBlackApocriton comp = apocriton.GetComp<CompBlackApocriton>();
                comp.inMission = true;
                GenPlace.TryPlaceThing(apocriton, sea.Center, sea, ThingPlaceMode.Direct, null, null, Rot4.North);
                Lord lord = LordMaker.MakeNewLord(apocriton.Faction, new LordJob_WaitAtPoint(sea.Center), sea);
                lord.AddPawn(apocriton);

                if (Find.World.GetComponent<MissionComponent>().BranchDict.TryGetValue("WillMilitorDie", true))
                {
                    IntVec3 graveVec = new IntVec3(37, 0, 34);
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.Sarcophagus, ThingDefOf.Uranium), graveVec, sea, ThingPlaceMode.Near);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref sea, "sea");
            Scribe_References.Look(ref site, "site");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref point, "point");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref desc, "desc");
            Scribe_Values.Look(ref inSignal, "inSignal");
        }
    }
}