using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityReleaseGas : CompProperties_AbilityEffect
{
	public GasType gasType;

	public float cellsToFill;

	public float durationSeconds;

	public int intervalTick = 30;

	public EffecterDef effecterReleasing;

	public CompProperties_AbilityReleaseGas()
	{
		compClass = typeof(CompAbilityReleaseGas);
	}
}
