using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.AI.Group;
using UnityEngine;

namespace GD3
{
    public class FinalBattleDummy : ThingWithComps
    {
        public List<IntVec3> cells;

        public bool detected;

        public int ticker;

        public int endTicker = -1;

        public Pawn apocriton;

        public int ProgressFull => 500 * 60;

        public int trueProgress;

        public int displayProgress;

        public float TruePercentage => Math.Min(trueProgress / (float)ProgressFull, 1);

        public float DisplayPercentage => Math.Min(displayProgress / (float)ProgressFull, 1);

        public float RemainingSeconds => (ProgressFull - trueProgress).TicksToSeconds();

        private BlackApocriton bossInt;

        public BlackApocriton Boss => bossInt == null ? bossInt = MapHeld?.mapPawns.AllPawnsSpawned.ToList().Find(p => p is BlackApocriton) as BlackApocriton : bossInt;

        public bool ReadyToEnd => TruePercentage >= 1f;

        private Effecter effecter;

        private IntVec3 tmpPos;

        private static Dictionary<IntRange, string> Strings
        {
            get
            {
                Dictionary<IntRange, string> strings = new Dictionary<IntRange, string>();
                strings.Add(new IntRange(0, 330), "GD.ApocritonTalk.0");
                strings.Add(new IntRange(330, 730), "GD.ApocritonTalk.1");
                strings.Add(new IntRange(730, 1200), "GD.ApocritonTalk.2");
                strings.Add(new IntRange(1200, 1800), "GD.ApocritonTalk.3");
                strings.Add(new IntRange(1800, 2200), "GD.ApocritonTalk.4");
                return strings;
            }
        }

        private static Dictionary<IntRange, string> EndStrings
        {
            get
            {
                Dictionary<IntRange, string> strings = new Dictionary<IntRange, string>();
                strings.Add(new IntRange(0, 900), "GD.MechHiveTalk.0");
                strings.Add(new IntRange(900, 1200), "GD.MechHiveTalk.1");
                return strings;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                cells = map.AllCells.ToList();
                cells.Remove(PositionHeld);
                cells.SortBy(c => c.DistanceTo(PositionHeld));
                map.terrainGrid.SetTerrain(PositionHeld, TerrainDefOf.Ice);
                Plant plant = (Plant)ThingMaker.MakeThing(GDDefOf.Plant_PeaceLily);
                plant.Growth = 1f;
                GenPlace.TryPlaceThing(plant, PositionHeld, map, ThingPlaceMode.Direct);
                map.weatherManager.TransitionTo(GDDefOf.GDBlizzard);
                Thing.allowDestroyNonDestroyable = true;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            Map map = MapHeld;
            if (!detected)
            {
                if (this.IsHashIntervalTick(120))
                {
                    Find.MusicManagerPlay.ForceFadeoutAndSilenceFor(120, 1.9f, false);
                    ChangeTerrain(map);
                }
                if (this.IsHashIntervalTick(20))
                {
                    if (map.mapPawns.AllPawnsSpawned.Any(p => p.Faction == Faction.OfPlayer && p.Position.InHorDistOf(PositionHeld, 9.9f)))
                    {
                        detected = true;
                        GDDefOf.IceSpreading.PlayOneShotOnCamera(map);
                        GDUtility.ExtraDrawer.StartDialog(Strings, true);
                    }
                }
            }
            else
            {
                ticker++;
                int count = cells.Any() ? (ticker <= 300 ? 3 : 10) : 0;
                for (int i = 0; i < count; i++)
                {
                    ChangeTerrain(map);
                }
                int lastTick = Strings.Last().Key.TrueMax;
                if (ticker == 300)
                {
                    GDUtility.ExtraDrawer.StartDraw(30);
                }
                else if (ticker == 330)
                {
                    apocriton = PawnGenerator.GeneratePawn(GDDefOf_Another.Mech_BlackApocriton, GDUtility.BlackMechanoid);
                    GenSpawn.Spawn(apocriton, PositionHeld, map);
                    apocriton.Rotation = Rot4.South;
                    Lord lord = LordMaker.MakeNewLord(apocriton.Faction, new LordJob_WaitAtPoint(PositionHeld), map);
                    lord.AddPawn(apocriton);
                }
                else if (ticker == lastTick)
                {
                    GDUtility.ExtraDrawer.StartDraw(30);
                    GDUtility.ExtraDrawer.pointer = this;
                    apocriton.GetLord()?.Cleanup();
                    Lord lord = LordMaker.MakeNewLord(apocriton.Faction, new LordJob_AssaultColony(apocriton.Faction), map);
                    lord.AddPawn(apocriton);
                    Thing.allowDestroyNonDestroyable = false;
                }
                else if (ticker > lastTick)
                {
                    int add = GetHealthProgress(GetHealthCondition());
                    trueProgress += add;
                    displayProgress += add;
                    if (displayProgress < trueProgress)
                    {
                        displayProgress = Math.Min(displayProgress + 5, trueProgress);
                    }
                }
            }

            int tickMax = BlackApocriton.Strings.Last().Key.TrueMax;
            if (endTicker >= 0 && endTicker < tickMax + 300)
            {
                endTicker++;
                if (effecter == null)
                {
                    effecter = GDDefOf.BlackApocritonDeath.Spawn(Boss, Boss.Map, new Vector3(0, 0, 0));
                }
                effecter.EffectTick(Boss, Boss);

                if (endTicker == tickMax - 240 || endTicker == tickMax - 100 || endTicker == tickMax - 30)
                {
                    GDUtility.ExtraDrawer.StartDraw(30);
                }
                else if (endTicker == tickMax)
                {
                    GDDefOf.GD_BigWave.SpawnMaintained(Boss.PositionHeld, map, 1f);
                    GDDefOf.HugePsychicWave.PlayOneShotOnCamera();
                    GenSpawn.Spawn(GDDefOf.BlackNanoChip, Boss.PositionHeld, map);
                    FleckMaker.Static(Boss.PositionHeld, map, FleckDefOf.PsycastAreaEffect, 6.0f);
                    FleckMaker.Static(Boss.DrawPos, map, FleckDefOf.PsycastSkipFlashEntry, 5);
                    FleckMaker.Static(Boss.PositionHeld, map, FleckDefOf.PsycastSkipInnerExit, 5);
                    Find.CameraDriver.shaker.DoShake(0.15f, 60);
                    EffecterDefOf.ImpactDustCloud.Spawn(Boss, Boss, 0.4f).Cleanup();
                    Find.World.GetComponent<MissionComponent>().scriptEnded = true;
                    Find.World.GetComponent<MainComponent>().list_str.Add("ScriptFinished");
                    tmpPos = Boss.PositionHeld;
                    Boss.Destroy();
                }
                else if (endTicker == tickMax + 300)
                {
                    TargetInfo info = new TargetInfo(tmpPos, Boss.Map);
                    GDUtility.SendSignal(GDUtility.GetQuestOfThing(this), "TargetKilled");
                    GDUtility.ExtraDrawer.DialogTitle = "GD.MechanoidHive";
                    GDUtility.ExtraDrawer.StartDialog(EndStrings);
                }
            }
        }

        private void ChangeTerrain(Map map)
        {
            if (!cells.NullOrEmpty())
            {
                IntVec3 pos = cells[0];
                cells.Remove(pos);
                map.terrainGrid.SetTerrain(pos, TerrainDefOf.Ice);
                for (int i = 0; i < 4; i++)
                {
                    FleckMaker.ThrowAirPuffUp(pos.ToVector3Shifted(), map);
                }
                List<Thing> things = map.thingGrid.ThingsListAt(pos).FindAll(t => t.Faction != Faction.OfPlayer);
                if (things.Any())
                {
                    for (int i = things.Count - 1; i >= 0; i--)
                    {
                        things[i].Destroy();
                    }
                }
                if (Rand.Chance(0.1f))
                {
                    Plant plant = (Plant)ThingMaker.MakeThing(GDDefOf.Plant_PeaceLily);
                    plant.Growth = 1f;
                    GenPlace.TryPlaceThing(plant, pos, map, ThingPlaceMode.Direct);
                }
            }
        }

        public int GetHealthCondition()
        {
            if (Boss == null)
            {
                return -1;
            }
            float health = Boss.health.summaryHealth.SummaryHealthPercent;
            if (health > 0.95f)
            {
                return 0;
            }
            else if (health > 0.6)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        public int GetHealthProgress(int index)
        {
            switch (index)
            {
                case 0:
                    return 1;
                case 1:
                    return 4;
                case 2:
                    return 10;
                default:
                    return 0;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref detected, "detected", false);
            Scribe_Values.Look(ref ticker, "ticker");
            Scribe_Values.Look(ref endTicker, "endTicker", -1);
            Scribe_Values.Look(ref trueProgress, "trueProgress");
            Scribe_Values.Look(ref displayProgress, "displayProgress");
            Scribe_Values.Look(ref tmpPos, "tmpPos");
            Scribe_References.Look(ref apocriton, "apocriton");
            Scribe_Collections.Look(ref cells, "cells", LookMode.Value);
        }
    }
}
