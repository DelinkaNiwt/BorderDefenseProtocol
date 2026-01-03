using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalAbilityReady : ThinkNode_Conditional
{
	public AbilityDef ability;

	protected override bool Satisfied(Pawn pawn)
	{
		if (ability == null)
		{
			return false;
		}
		Log.Message("ThinkNode_ConditionalAbilityReady " + !pawn.abilities.GetAbility(ability, includeTemporary: true).OnCooldown);
		return !pawn.abilities.GetAbility(ability, includeTemporary: true).OnCooldown;
	}
}
