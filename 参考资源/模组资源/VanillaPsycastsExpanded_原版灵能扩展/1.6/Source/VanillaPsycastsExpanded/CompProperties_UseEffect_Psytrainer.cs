using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class CompProperties_UseEffect_Psytrainer : CompProperties_UseEffectGiveAbility
{
	public CompProperties_UseEffect_Psytrainer()
	{
		((CompProperties)this).compClass = typeof(CompPsytrainer);
	}
}
