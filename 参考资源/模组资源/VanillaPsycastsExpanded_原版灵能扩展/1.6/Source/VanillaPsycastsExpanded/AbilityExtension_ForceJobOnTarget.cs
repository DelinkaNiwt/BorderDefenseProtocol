using RimWorld.Planet;
using VEF.Abilities;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_ForceJobOnTarget : AbilityExtension_ForceJobOnTargetBase
{
	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo target in targets)
		{
			ForceJob(target, ability);
		}
	}
}
