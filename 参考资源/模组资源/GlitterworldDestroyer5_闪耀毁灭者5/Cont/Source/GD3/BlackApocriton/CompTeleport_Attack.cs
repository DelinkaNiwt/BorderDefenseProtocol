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
    public class CompTeleport_Attack : ThingComp
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
                if (!(p.mindState.enemyTarget is Pawn pv))
                {
                    return false;
                }
                if (pv.Map != p.Map || !pv.Position.IsValid || pv.Position.DistanceTo(p.Position) < 2.9f || pv.Position.DistanceTo(p.Position) > this.radius || !pv.CanReach(p, PathEndMode.Touch, Danger.Deadly))
                {
                    return false;
                }
                return true;
            }
        }

        public Pawn target
        {
            get
            {
                Pawn p = this.parent as Pawn;
                if (p != null && p.mindState != null && p.mindState.enemyTarget != null && p.mindState.enemyTarget is Pawn pv)
                {
                    if (!pv.Position.IsValid || !pv.Position.Standable(pv.Map))
                    {
                        return null;
                    }
                    return pv;
                }
                return null;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (ticks > 0)
            {
                ticks--;
                return;
            }
            if (!AICanUse)
            {
                return;
            }
            System.Random random = new System.Random();
            int i = random.Next(0, 99);
            if (i > 10)
            {
                return;
            }
            TeleportTo(target.Position);
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
            effecter = EffecterDefOf.Skip_Entry.Spawn(target, target.Map, 1f);
            effecter.Trigger(target, target, -1);
            effecter.Cleanup();
            effecter = EffecterDefOf.Skip_Entry.Spawn(target, target.Map, 1f);
            effecter.Trigger(target, target, -1);
            effecter.Cleanup();

            pawn.stances.stunner.StunFor(15, pawn, false, false);
            pawn.Notify_Teleported(true, true);
            CompAbilityEffect_Teleport.SendSkipUsedSignal(pawn.Position, pawn);

            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(vec, pawn.Map, false));

            ticks = 240;
        }

        public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
		}

		public int ticks = 0;

        private readonly float radius = 25.9f;
    }
}
