using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class CompAbilityEffect_GroupRepair : CompAbilityEffect
{
	private static List<Hediff> tmpHediffs = new List<Hediff>();

	private const string MechRepairEffectDefName = "MechResurrected";

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (!target.Cell.IsValid)
		{
			return;
		}
		Map map = parent.pawn.Map;
		Faction faction = parent.pawn.Faction;
		foreach (Thing thing in GenRadial.RadialDistinctThingsAround(target.Cell, map, 25f, useCenter: true))
		{
			if (thing is Pawn pawn && pawn.Faction == faction && pawn.RaceProps.IsMechanoid)
			{
				RepairMech(pawn);
				SpawnRepairEffect(pawn.Position, map);
			}
		}
	}

	private void SpawnRepairEffect(IntVec3 position, Map map)
	{
		EffecterDef effecterDef = DefDatabase<EffecterDef>.GetNamedSilentFail("MechResurrected");
		if (effecterDef != null)
		{
			Effecter effecter = effecterDef.Spawn();
			effecter.Trigger(new TargetInfo(position, map), TargetInfo.Invalid);
			effecter.Cleanup();
		}
		else
		{
			FleckMaker.ThrowLightningGlow(position.ToVector3(), map, 1.5f);
			FleckMaker.ThrowSmoke(position.ToVector3(), map, 1f);
		}
	}

	private void RepairMech(Pawn pawn)
	{
		tmpHediffs.Clear();
		tmpHediffs.AddRange(pawn.health.hediffSet.hediffs);
		tmpHediffs.SortBy((Hediff injury) => 0f - injury.Severity);
		for (int num = 0; num < tmpHediffs.Count && num < 5; num++)
		{
			Hediff hediff = tmpHediffs[num];
			if (hediff != null && (hediff is Hediff_Injury || hediff is Hediff_MissingPart))
			{
				pawn.health.RemoveHediff(hediff);
			}
		}
		tmpHediffs.Clear();
	}
}
