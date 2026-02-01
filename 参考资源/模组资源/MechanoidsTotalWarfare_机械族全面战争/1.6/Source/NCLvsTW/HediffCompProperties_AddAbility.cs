using RimWorld;
using Verse;

public class HediffCompProperties_AddAbility : HediffCompProperties
{
	public AbilityDef abilityDef;

	public HediffCompProperties_AddAbility()
	{
		compClass = typeof(HediffComp_AddAbility);
	}
}
