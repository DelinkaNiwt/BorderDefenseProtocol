using RimWorld;
using Verse;

namespace NyarsModPackOne;

public class CompProperties_AbilitySpawnDrone : CompProperties_AbilityEffect
{
	public int droneCount = 1;

	public PawnKindDef droneKind;

	public CompProperties_AbilitySpawnDrone()
	{
		compClass = typeof(CompAbilityEffect_SpawnDrone);
	}
}
