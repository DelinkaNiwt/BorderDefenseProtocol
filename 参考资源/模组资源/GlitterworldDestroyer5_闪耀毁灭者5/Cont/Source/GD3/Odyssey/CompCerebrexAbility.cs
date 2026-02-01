using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class CompCerebrexAbility : ThingComp
    {
        public ThingWithComps Core => parent;

        public CompCerebrexCore compCore;

        public CompCerebrexCore CompCore => compCore == null ? compCore = Core.TryGetComp<CompCerebrexCore>() : compCore;

        public int life;

        private static SimpleCurve pointCurve = new SimpleCurve
        {
            new CurvePoint(0, 1000),
            new CurvePoint(30000, 8000),
        };

        public static int AbilityCooldown = 3600;

        public int lastCallReinforceTick = -1;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!respawningAfterLoad)
            {
                lastCallReinforceTick = Find.TickManager.TicksGame;
            }
        }

        public override void CompTick()
        {
            life++;
            if (life == 1)
            {
                CompCore.AssemblerLord.faction = Core.Faction;
                List<Building> buildings = Core.Map.listerBuildings.allBuildingsNonColonist;
                foreach (Building building in buildings)
                {
                    CompMechhiveAssembler assembler = building.TryGetComp<CompMechhiveAssembler>();
                    if (assembler != null)
                    {
                        typeof(CompMechhiveAssembler).GetField("core", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(assembler, CompCore);
                    }
                }
            }
            bool active = !(bool)typeof(CompCerebrexCore).GetField("deactivated", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(CompCore);
            if (active)
            {
                IntVec3 pos = Core.TrueCenter().ToIntVec3();
                Map map = Core.Map;
                if (Core.IsHashIntervalTick(60))
                {
                    FleckMaker.Static(pos, map, FleckDefOf.PsycastAreaEffect, 8.9f);
                }
                int timer = Find.TickManager.TicksGame - lastCallReinforceTick;
                if (timer == AbilityCooldown - 600)
                {
                    Messages.Message("GD.CerebrexAbilityWarning".Translate(), Core, MessageTypeDefOf.NegativeEvent);
                }
                else if (timer == AbilityCooldown)
                {
                    int index = Rand.Range(0, 5);
                    if (index == 0 || index == 1) CallLandReinforce();
                    else if (index == 2) CallAirReinforce();
                    else if (index == 3) RandomSkip();
                    else if (index == 4) PsychicVertigo();
                }
            }
        }

        public void CallLandReinforce()
        {
            Map map = Core.Map;
            IncidentParms parms = new IncidentParms();
            parms.target = map;
            parms.points = pointCurve.Evaluate(life);
            parms.faction = Faction.OfMechanoids;
            parms.pawnGroupKind = PawnGroupKindDefOf.Combat;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms);
            List<Pawn> pawns = PawnGroupMakerUtility.GeneratePawns(defaultPawnGroupMakerParms).ToList();
            if (!pawns.NullOrEmpty())
            {
                Predicate<IntVec3, List<Pawn>> validator = delegate (IntVec3 c, List<Pawn> enemies)
                {
                    if (!c.InBounds(map) || !c.Standable(map))
                    {
                        return false;
                    }
                    if (!enemies.Any())
                    {
                        return false;
                    }
                    enemies.SortBy(p => p.PositionHeld.DistanceTo(c));
                    if (enemies[0].PositionHeld.DistanceTo(c) <= 2.9f)
                    {
                        return false;
                    }
                    return true;
                };
                List<Pawn> colonists = map.mapPawns.AllPawnsSpawned.ToList().FindAll(p => p.HostileTo(Faction.OfMechanoids));
                if (colonists.Any())
                {
                    Pawn victim = colonists.RandomElement();
                    List<IntVec3> cells = GenRadial.RadialCellsAround(victim.Position, 14.9f, false).ToList().FindAll(c => validator(c, colonists));
                    if (cells.Any())
                    {
                        foreach (Pawn p in pawns)
                        {
                            IntVec3 c = cells.RandomElement();
                            SkipUtility.SkipTo(p, c, map);
                            FleckMaker.Static(c, map, FleckDefOf.PsycastSkipFlashEntry, 1);
                            FleckMaker.Static(c, map, FleckDefOf.PsycastSkipInnerExit, 1);
                            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(p);
                            p.stances.stunner.StunFor(180, p);
                        }
                        Lord lord = FindLordToJoin(pawns[0], typeof(LordJob_AssaultColony));
                        if (lord == null)
                        {
                            lord = CreateNewLord(pawns[0], true, -1, typeof(LordJob_AssaultColony));
                        }
                        lord.AddPawns(pawns);
                        SoundInfo info = SoundInfo.OnCamera();
                        info.volumeFactor = 0.6f;
                        GDDefOf.HugePsychicWave.PlayOneShot(info);
                        Find.LetterStack.ReceiveLetter("GD.CerebrexLandReinforce".Translate(), "GD.CerebrexLandReinforceDesc".Translate(victim.LabelShort, pawns.Count), LetterDefOf.ThreatSmall, victim);
                        lastCallReinforceTick = Find.TickManager.TicksGame;
                    }
                }
            }
        }

        public void CallAirReinforce()
        {
            Map map = Core.Map;
            List<Pawn> colonists = map.mapPawns.AllPawnsSpawned.ToList().FindAll(p => p.HostileTo(Faction.OfMechanoids));
            if (colonists.Any())
            {
                Pawn victim = colonists.RandomElement();
                GDUtility.CallForReinforcement(Core.TrueCenter().ToIntVec3(), map, pointCurve.Evaluate(life), delegate(TargetInfo tar)
                {
                    SoundInfo info = SoundInfo.OnCamera();
                    info.volumeFactor = 0.6f;
                    GDDefOf.HugePsychicWave.PlayOneShot(info);
                });
                Find.LetterStack.ReceiveLetter("GD.CerebrexAirReinforce".Translate(), "GD.CerebrexAirReinforceDesc".Translate(), LetterDefOf.ThreatSmall, Core);
                lastCallReinforceTick = Find.TickManager.TicksGame;
            }
        }

        public void RandomSkip()
        {
            Map map = Core.Map;
            List<Pawn> colonists = map.mapPawns.AllPawnsSpawned.ToList().FindAll(p => p.HostileTo(Faction.OfMechanoids));
            Predicate<IntVec3> validator = delegate (IntVec3 c)
            {
                if (!c.InBounds(map) || !c.Standable(map))
                {
                    return false;
                }
                return true;
            };
            if (colonists.Any())
            {
                foreach (Pawn p in colonists)
                {
                    IntVec3 tar = GenRadial.RadialCellsAround(p.PositionHeld, 7.9f, false).ToList().FindAll(validator).RandomElement();
                    if (tar.IsValid)
                    {
                        SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(p.PositionHeld, map));
                        FleckMaker.Static(p.PositionHeld, map, FleckDefOf.PsycastSkipFlashEntry, 1);
                        FleckMaker.Static(tar, map, FleckDefOf.PsycastSkipInnerExit, 1);
                        SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(tar, map));
                        FleckMaker.ConnectingLine(p.DrawPos, tar.ToVector3Shifted(), GDDefOf.GDSwapLine, map, 1f);
                        SkipUtility.SkipTo(p, tar, map);
                    }
                }
                SoundInfo info = SoundInfo.OnCamera();
                info.volumeFactor = 0.6f;
                GDDefOf.HugePsychicWave.PlayOneShot(info);
                Find.LetterStack.ReceiveLetter("GD.CerebrexRandomSkip".Translate(), "GD.CerebrexRandomSkipDesc".Translate(), LetterDefOf.ThreatSmall, Core);
                lastCallReinforceTick = Find.TickManager.TicksGame;
            }
        }

        public void PsychicVertigo()
        {
            Map map = Core.Map;
            List<Pawn> colonists = map.mapPawns.AllPawnsSpawned.ToList().FindAll(p => p.HostileTo(Faction.OfMechanoids));
            if (colonists.Any())
            {
                foreach (Pawn p in colonists)
                {
                    if (!p.RaceProps.IsFlesh)
                    {
                        continue;
                    }
                    FleckMaker.Static(p.DrawPos, map, FleckDefOf.PsycastAreaEffect, 2.9f);
                    DamageWorker_PsychicStrike.AddHediff(p, GDDefOf.PsychicVertigo, 900);
                }
                SoundInfo info = SoundInfo.OnCamera();
                info.volumeFactor = 0.6f;
                GDDefOf.HugePsychicWave.PlayOneShot(info);
                Find.LetterStack.ReceiveLetter("GD.CerebrexVertigo".Translate(), "GD.CerebrexVertigoDesc".Translate(), LetterDefOf.ThreatSmall, Core);
                lastCallReinforceTick = Find.TickManager.TicksGame;
            }
        }

        public Lord FindLordToJoin(Pawn pawn, Type lordJobType)
        {
            if (pawn.Spawned && pawn.Map != null)
            {
                Predicate<Pawn> hasJob = delegate (Pawn x)
                {
                    Lord lord2 = x.GetLord();
                    return lord2 != null && lord2.LordJob.GetType() == lordJobType;
                };
                List<Pawn> list = pawn.Map.mapPawns.AllPawnsSpawned.ToList().FindAll(p => p.Faction == pawn.Faction);
                Pawn foundPawn = list.Find(hasJob);
                if (foundPawn != null)
                {
                    return foundPawn.GetLord();
                }
            }
            return null;
        }

        public static Lord CreateNewLord(Pawn byPawn, bool aggressive, float defendRadius, Type lordJobType)
        {
            IntVec3 result;
            if (byPawn.Position.Standable(byPawn.Map))
            {
                result = byPawn.Position;
            }
            else if (!CellFinder.TryFindRandomCellNear(byPawn.Position, byPawn.Map, 5, (IntVec3 c) => c.Standable(byPawn.Map) && byPawn.Map.reachability.CanReach(c, byPawn, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors)), out result))
            {
                Log.Error("Found no place for pawns to defend " + byPawn);
                result = IntVec3.Invalid;
            }
            return LordMaker.MakeNewLord(byPawn.Faction, Activator.CreateInstance(lordJobType, new SpawnedPawnParams
            {
                aggressive = aggressive,
                defendRadius = defendRadius,
                defSpot = result,
                spawnerThing = byPawn
            }) as LordJob, byPawn.Map);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action random = new Command_Action
                {
                    defaultLabel = "DEV: Random ability",
                    action = delegate
                    {
                        lastCallReinforceTick = Find.TickManager.TicksGame - AbilityCooldown + 1;
                    }
                };
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = "DEV: Call land reinforce",
                    action = delegate
                    {
                        CallLandReinforce();
                    }
                };
                Command_Action command_Action2 = new Command_Action
                {
                    defaultLabel = "DEV: Call air reinforce",
                    action = delegate
                    {
                        CallAirReinforce();
                    }
                };
                Command_Action command_Action3 = new Command_Action
                {
                    defaultLabel = "DEV: Random skip",
                    action = delegate
                    {
                        RandomSkip();
                    }
                };
                Command_Action command_Action4 = new Command_Action
                {
                    defaultLabel = "DEV: Psychic vertigo",
                    action = delegate
                    {
                        PsychicVertigo();
                    }
                };
                yield return random;
                yield return command_Action;
                yield return command_Action2;
                yield return command_Action3;
                yield return command_Action4;
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref life, "life");
            Scribe_Values.Look(ref lastCallReinforceTick, "lastCallReinforceTick");
        }
    }
}
