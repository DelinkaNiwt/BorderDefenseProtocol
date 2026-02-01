using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Empath;

public class AbilityExtension_EnergyDump : AbilityExtension_AbilityMod
{
	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (globalTargetInfo.Thing is Pawn pawn && pawn.needs?.rest != null)
			{
				pawn.needs.rest.CurLevel = 1f;
				ability.pawn.needs.rest.CurLevel = 0f;
			}
		}
	}
}
