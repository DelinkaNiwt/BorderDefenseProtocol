using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_AbilityGiveMapHediff : CompProperties_AbilityGiveHediff
{
	public bool ignorePawnsInSameFaction;

	public bool onlyPawnsInSameFaction;

	public List<PawnKindDef> inavailablePawnKinds;

	public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
	{
		foreach (string item in base.ConfigErrors(parentDef))
		{
			yield return item;
		}
		if (ignorePawnsInSameFaction && onlyPawnsInSameFaction)
		{
			yield return "ignorePawnsInSameFaction and onlyPawnsInSameFaction are both TRUE, causing ability to have no effect.";
		}
	}

	public CompProperties_AbilityGiveMapHediff()
	{
		compClass = typeof(CompAbilityEffect_GiveMapHediff);
	}
}
