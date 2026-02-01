using RimWorld;
using Verse;

namespace NCL;

[DefOf]
public static class NCLContainerDefOf
{
	public static ThingDef Building_PawnContainer;

	public static HediffDef PawnContainedHediff;

	public static JobDef TurnIntoBuilding;

	public static AbilityDef TurnIntoBuildingAbility;

	public static ThingDef Building_PawnContainerForged;

	public static AbilityDef TurnIntoBuildingAbilityForged;

	public static JobDef TurnIntoBuildingForged;

	static NCLContainerDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(NCLContainerDefOf));
	}
}
