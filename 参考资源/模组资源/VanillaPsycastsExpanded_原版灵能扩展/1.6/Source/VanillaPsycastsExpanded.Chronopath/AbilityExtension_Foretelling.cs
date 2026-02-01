using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

public class AbilityExtension_Foretelling : AbilityExtension_GiveInspiration
{
	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		if (Rand.Chance(0.5f))
		{
			base.Cast(targets, ability);
			return;
		}
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (globalTargetInfo.Thing is Pawn pawn)
			{
				pawn.needs.mood.thoughts.memories.TryGainMemoryFast(VPE_DefOf.VPE_Future);
			}
		}
	}
}
