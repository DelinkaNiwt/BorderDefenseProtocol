using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

public class AbilityExtension_Age : AbilityExtension_AbilityMod
{
	public float? casterYears;

	public float? targetYears;

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		if (casterYears.HasValue)
		{
			Age(ability.pawn, casterYears.Value);
		}
		if (!targetYears.HasValue)
		{
			return;
		}
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (globalTargetInfo.Thing is Pawn pawn)
			{
				Age(pawn, targetYears.Value);
			}
		}
	}

	public override bool CanApplyOn(LocalTargetInfo target, Ability ability, bool throwMessages = false)
	{
		if (!((AbilityExtension_AbilityMod)this).CanApplyOn(target, ability, throwMessages))
		{
			return false;
		}
		if (!targetYears.HasValue)
		{
			return true;
		}
		if (!(target.Thing is Pawn pawn))
		{
			return false;
		}
		if (!pawn.RaceProps.IsFlesh)
		{
			return false;
		}
		if (!pawn.RaceProps.Humanlike)
		{
			return false;
		}
		return true;
	}

	public static void Age(Pawn pawn, float years)
	{
		pawn.ageTracker.AgeBiologicalTicks += Mathf.FloorToInt(years * 3600000f);
		if (years < 0f)
		{
			List<HediffGiverSetDef> hediffGiverSets = pawn.def.race.hediffGiverSets;
			if (hediffGiverSets == null)
			{
				return;
			}
			float x = (float)pawn.ageTracker.AgeBiologicalYears / pawn.def.race.lifeExpectancy;
			foreach (HediffGiverSetDef item in hediffGiverSets)
			{
				foreach (HediffGiver hediffGiver in item.hediffGivers)
				{
					if (hediffGiver is HediffGiver_Birthday hediffGiver_Birthday && hediffGiver_Birthday.ageFractionChanceCurve.Evaluate(x) <= 0f)
					{
						Hediff firstHediffOfDef;
						while ((firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(hediffGiver_Birthday.hediff)) != null)
						{
							pawn.health.RemoveHediff(firstHediffOfDef);
						}
					}
				}
			}
		}
		if ((float)pawn.ageTracker.AgeBiologicalYears > pawn.def.race.lifeExpectancy * 1.1f && (pawn.genes == null || pawn.genes.HediffGiversCanGive(VPE_DefOf.HeartAttack)))
		{
			BodyPartRecord bodyPartRecord = pawn.RaceProps.body.AllParts.FirstOrDefault((BodyPartRecord p) => p.def == BodyPartDefOf.Heart);
			Hediff hediff = HediffMaker.MakeHediff(VPE_DefOf.HeartAttack, pawn, bodyPartRecord);
			hediff.Severity = 1.1f;
			pawn.health.AddHediff(hediff, bodyPartRecord);
		}
	}
}
