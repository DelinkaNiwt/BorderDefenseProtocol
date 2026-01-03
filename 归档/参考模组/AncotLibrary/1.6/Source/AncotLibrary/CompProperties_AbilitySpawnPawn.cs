using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilitySpawnPawn : CompProperties_AbilityEffect
{
	public PawnKindDef pawnKind;

	public int spawnCount;

	public int spawnAge = 0;

	public HediffDef hediffAddToSpawnPawn;

	public bool setFaction = true;

	public EffecterDef effect;

	public CompProperties_AbilitySpawnPawn()
	{
		compClass = typeof(CompAbilitySpawnPawn);
	}
}
