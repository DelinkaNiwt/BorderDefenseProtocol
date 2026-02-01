using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse.Sound;
using Verse.AI;

namespace GD3
{
    public class CompTeleport_Defend : ThingComp
    {
        public bool AICanUse
        {
            get
            {
                Pawn p = this.parent as Pawn;
                if (ticks > 0)
                {
                    return false;
                }
                if (p == null || !p.Spawned || p.Dead || p.Downed || p.mindState.mentalStateHandler.InMentalState || p.Faction == null)
                {
                    return false;
                }
                Pawn pv = (p.mindState.enemyTarget as Pawn) ?? p.mindState.meleeThreat;
                if (pv == null)
                {
                    return false;
                }
                if (pv.Map != p.Map || !pv.Position.IsValid || pv.Position.DistanceTo(p.Position) >= radius || !pv.CanReach(p, PathEndMode.Touch, Danger.Deadly))
                {
                    return false;
                }
                return true;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = this.parent as Pawn;
            if (ticks > 0)
            {
                ticks--;
                return;
            }
            if (pawn.IsHashIntervalTick(80))
            {
                if (!AICanUse)
                {
                    return;
                }

                List<Thing> hostileThings = pawn.MapHeld.listerThings.AllThings.FindAll(t => t.HostileTo(pawn));
                nextExplosionCell = CellFinderLoose.GetFleeDest(pawn, hostileThings, range);
                TeleportTo(nextExplosionCell);
            }
        }

        public void TeleportTo(IntVec3 vec)
        {
            Pawn pawn = this.parent as Pawn;
            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));

            Effecter effecter = EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map, 1f);
            effecter.Trigger(pawn, pawn, -1);
            effecter.Cleanup();
            effecter = EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map, 1f);
            effecter.Trigger(pawn, pawn, -1);
            effecter.Cleanup();
            FleckMaker.ConnectingLine(pawn.DrawPos, vec.ToVector3(), GDDefOf.PsycastPsychicLine, pawn.Map, 1f);
            pawn.Position = vec;
            effecter = EffecterDefOf.Skip_Entry.Spawn(vec, pawn.Map, 1f);
            effecter.Trigger(new TargetInfo(vec, pawn.Map), new TargetInfo(vec, pawn.Map), -1);
            effecter.Cleanup();
            effecter = EffecterDefOf.Skip_Entry.Spawn(vec, pawn.Map, 1f);
            effecter.Trigger(new TargetInfo(vec, pawn.Map), new TargetInfo(vec, pawn.Map), -1);
            effecter.Cleanup();

            pawn.stances.stunner.StunFor(15, pawn, false, false);
            pawn.Notify_Teleported(true, true);
            CompAbilityEffect_Teleport.SendSkipUsedSignal(pawn.Position, pawn);

            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(vec, pawn.Map, false));

            ticks = 600;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
        }

        public int ticks = 0;

        private readonly static float radius = 6.9f;

        private readonly static float range = 24.9f;

        private static List<Thing> tmpHostileSpots = new List<Thing>();

        private IntVec3 nextExplosionCell = IntVec3.Invalid;

        public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(1f, 0.8f)
        };
    }
}
