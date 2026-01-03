using RimWorld;
using Verse;

namespace Milira;

public class CompProperties_AbilityLaunchBroadShieldUnit : CompProperties_AbilityEffect
{
	public ThingDef projectileDef;

	public CompProperties_AbilityLaunchBroadShieldUnit()
	{
		compClass = typeof(CompAbilityEffect_LaunchBroadShieldUnit);
	}
}
