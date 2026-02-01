using RimWorld.Planet;
using VEF.Abilities;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_JoinFaction : AbilityExtension_AbilityMod
{
	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			if (globalTargetInfo.Thing.Faction != ability.pawn.Faction)
			{
				globalTargetInfo.Thing.SetFaction(ability.pawn.Faction);
			}
		}
	}
}
