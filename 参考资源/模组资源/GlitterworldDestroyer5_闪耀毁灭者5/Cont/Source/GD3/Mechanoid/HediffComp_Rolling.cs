using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
	public class HediffComp_Rolling : HediffComp
	{
		public HediffCompProperties_Rolling Props
		{
			get
			{
				return (HediffCompProperties_Rolling)this.props;
			}
		}

		public bool CanApply
        {
            get
            {
				Pawn pawn = base.Pawn;
				if (pawn.Spawned && !pawn.DeadOrDowned && pawn.Faction != null)
                {
                    if (pawn.Flying)
                    {
                        return false;
                    }
                    if (!pawn.Faction.IsPlayer)
                    {
						return true;
                    }
                    if (Props.requireTech != null)
                    {
                        return Props.requireTech.IsFinished;
                    }
                    else
                    {
                        return true;
                    }
                }
				return false;
            }
        }

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			Pawn pawn = base.Pawn;
			if (this.CanApply)
			{
				this.ticks++;
				if (this.ticks >= this.Props.interval)
				{
                    IEnumerable<Pawn> enumerable = from x in this.Pawn.Map.mapPawns.AllPawnsSpawned
                                                   where x.Position.DistanceTo(pawn.Position) <= 2.9f && x.HostileTo(pawn)
                                                   select x;
                    List<Pawn> pawns = enumerable.ToList();
                    pawns.RemoveAll(p => p.RaceProps.baseBodySize > Props.maxBodySize || p.IsPrisoner || p.Flying);
                    if (ModsConfig.AnomalyActive)
                    {
                        pawns.RemoveAll(p => p.RaceProps.IsAnomalyEntity && p.IsOnHoldingPlatform);
                    }
                    if (!pawns.NullOrEmpty())
                    {
                        for (int i = 0; i < pawns.Count; i++)
                        {
                            Pawn victim = pawns[i];
                            if (victim != null && victim.Spawned && !victim.Dead)
                            {
                                this.DamageVictim(victim);
                                if (victim.RaceProps.IsMechanoid)
                                {
                                    SoundDefOf.Pawn_Melee_Punch_HitBuilding_Generic.PlayOneShot(new TargetInfo(victim.PositionHeld, victim.MapHeld, false)); 
                                }
                                else
                                {
                                    SoundDefOf.Pawn_Melee_Punch_HitPawn.PlayOneShot(new TargetInfo(victim.PositionHeld, victim.MapHeld, false));
                                }
                            }
                        }
                    }
                    this.ticks = 0;
				}
			}
		}

        private void DamageVictim(Pawn victim)
        {
            if (victim.Dead)
            {
                return;
            }
            HediffSet hediffSet = victim.health.hediffSet;
            IEnumerable<BodyPartRecord> source = from x in HittablePartsViolence(hediffSet)
                                                 where !victim.health.hediffSet.hediffs.Any((Hediff y) => y.Part == x && y.CurStage != null && y.CurStage.partEfficiencyOffset < 0f)
                                                 select x;
            BodyPartRecord bodyPartRecord = source.RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
            if (bodyPartRecord == null)
            {
                return;
            }
            int maxHitPoints = bodyPartRecord.def.hitPoints;
            float num = this.Props.damage;
            if (victim.BodySize <= 1.0f)
            {
                num *= 3.5f;
            }
            Pawn attacker = this.Pawn;
            victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, num, Props.penetration, 0f, attacker, bodyPartRecord, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
        }

        private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
        {
            return from x in bodyModel.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
                   where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
                   select x;
        }
        private int ticks;
	}
}