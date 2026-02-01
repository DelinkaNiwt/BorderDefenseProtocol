using System;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace GD3
{
    public class DamageWorker_PsychicStrike : DamageWorker
    {
		public override DamageResult Apply(DamageInfo dinfo, Thing victim)
		{
			DamageResult damageResult = base.Apply(dinfo, victim);
			damageResult.stunned = true;
			if (dinfo.Instigator == null || victim == null || !(dinfo.Instigator is Pawn) || !(victim is Pawn))
            {
				DamageResult damageResult0 = new DamageResult();
				return damageResult0;
            }
			if (dinfo.Instigator.Faction != null && victim.Faction != null)
            {
				Pawn attacker = dinfo.Instigator as Pawn;
				Pawn vict = victim as Pawn;
				if (attacker.Faction.HostileTo(vict.Faction))
                {
					if (vict.RaceProps.IsMechanoid)
                    {
						AddHediff(vict, GDDefOf.PsychicVertigo, 300);
						return damageResult;
					}
					float psy = vict.GetStatValue(StatDefOf.PsychicSensitivity);
					if (psy == 0f)
                    {
						return damageResult;
					}
					else if (psy > 0f && psy < 2f)
                    {
						AddHediff(vict, GDDefOf.PsychicVertigo, (int)(900 * psy));
					}
					else if (psy >= 2f && psy < 3f)
                    {
						AddHediff(vict, GDDefOf.PsychicVertigo, (int)(900 * psy));
						DoEffectOn(vict);
					}
                    else if (psy >= 3f && psy < 4f)
					{
						AddHediff(vict, GDDefOf.PsychicVertigo, (int)(900 * psy));
						DoEffectOn(vict);
						DamageVictim(attacker, vict, dinfo.Weapon, 0.4f);
					}
					else
					{
						DamageVictim(attacker, vict, dinfo.Weapon, 30f);
					}
				}
                else
                {
					DamageResult damageResult0 = new DamageResult();
					return damageResult0;
				}
            }
			return damageResult;
		}

		public static void AddHediff(Pawn pawn, HediffDef hediffDef, int ticks)
		{
			Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef, false);
			if (hediff == null)
			{
				hediff = pawn.health.AddHediff(hediffDef, pawn.health.hediffSet.GetBrain(), null, null);
				hediff.Severity = 1f;
			}
			HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
			if (hediffComp_Disappears != null)
			{
				hediffComp_Disappears.ticksToDisappear = ticks;
			}
		}

		private void DoEffectOn(Thing target)
		{
			Pawn pawn = (Pawn)target;
			if (!pawn.Dead)
			{
				Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, pawn);
				pawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).TryRandomElement(out var result);
				pawn.health.AddHediff(hediff, result);
			}
		}

		public void DamageVictim(Pawn attacker, Pawn victim, ThingDef weapon, float multiplier)
		{
			if (victim.Dead)
			{
				return;
			}
			HediffSet hediffSet = victim.health.hediffSet;
			victim.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).TryRandomElement(out var result);
			BodyPartRecord brain = result;
			IEnumerable <BodyPartRecord> source = from x in HittablePartsViolence(hediffSet)
												 where !victim.health.hediffSet.hediffs.Any((Hediff y) => y.Part == x && y.CurStage != null && y.CurStage.partEfficiencyOffset < 0f)
												 select x;
			BodyPartRecord bodyPartRecord = brain ?? source.RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
			if (bodyPartRecord == null)
			{
				return;
			}
			int maxHitPoints = bodyPartRecord.def.hitPoints;
			victim.TakeDamage(new DamageInfo(DamageDefOf.Burn, (float)(maxHitPoints - 1) * multiplier, 9999, 0f, attacker, bodyPartRecord, weapon, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
		}

		private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
		{
			return from x in bodyModel.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
				   where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
				   select x;
		}
	}
}
