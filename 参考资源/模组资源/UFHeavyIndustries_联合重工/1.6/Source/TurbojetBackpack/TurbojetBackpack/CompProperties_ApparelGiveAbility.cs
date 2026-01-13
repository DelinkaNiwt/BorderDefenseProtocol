using RimWorld;
using Verse;

namespace TurbojetBackpack;

public class CompProperties_ApparelGiveAbility : CompProperties
{
	public AbilityDef abilityDef;

	public CompProperties_ApparelGiveAbility()
	{
		compClass = typeof(CompApparelGiveAbility);
	}
}
