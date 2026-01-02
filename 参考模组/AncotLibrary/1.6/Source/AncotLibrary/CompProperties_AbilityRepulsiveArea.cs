using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityRepulsiveArea : CompProperties_AbilityEffect
{
	public int distance = 10;

	public float radius = 8f;

	public bool applyOnAlly = true;

	public bool applyOnAllyOnly = false;

	public bool applyOnMech = true;

	public bool targetOnCaster;

	public EffecterDef effecter;

	public List<HediffDef> removeHediffsAffected;

	public CompProperties_AbilityRepulsiveArea()
	{
		compClass = typeof(CompAbilityRepulsiveArea);
	}
}
