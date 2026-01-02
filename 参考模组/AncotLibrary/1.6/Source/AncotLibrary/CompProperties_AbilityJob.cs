using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityJob : CompProperties_AbilityEffect
{
	public JobDef jobDef;

	public CompProperties_AbilityJob()
	{
		compClass = typeof(CompAbilityJob);
	}
}
