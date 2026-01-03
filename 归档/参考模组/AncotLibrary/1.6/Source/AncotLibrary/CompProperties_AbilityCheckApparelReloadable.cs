using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityCheckApparelReloadable : CompProperties_AbilityEffect
{
	public ThingDef apparel;

	public int consumeChargeAmount = 10;

	public CompProperties_AbilityCheckApparelReloadable()
	{
		compClass = typeof(CompAbilityCheckApparelReloadable);
	}

	public override IEnumerable<string> ExtraStatSummary()
	{
		yield return "Ancot.AbilityChargeCost".Translate(consumeChargeAmount.ToString());
	}
}
