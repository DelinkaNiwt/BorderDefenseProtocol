using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class SoulFromSky : Skyfaller
{
	public Corpse target;

	protected override void Impact()
	{
		Pawn innerPawn = target.InnerPawn;
		List<Hediff> hediffs = innerPawn.health.hediffSet.hediffs;
		for (int num = hediffs.Count - 1; num >= 0; num--)
		{
			Hediff hediff = hediffs[num];
			if (hediff is Hediff_MissingPart { Part: var part })
			{
				innerPawn.health.RemoveHediff(hediff);
				innerPawn.health.RestorePart(part);
			}
			else if (hediff.def != VPE_DefOf.TraumaSavant && (hediff.def.isBad || hediff is Hediff_Addiction) && hediff.def.everCurableByItem)
			{
				innerPawn.health.RemoveHediff(hediff);
			}
		}
		ResurrectionUtility.TryResurrectWithSideEffects(innerPawn);
		if (!innerPawn.Spawned)
		{
			GenSpawn.Spawn(innerPawn, base.Position, base.MapHeld);
		}
		Destroy();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref target, "target");
	}
}
