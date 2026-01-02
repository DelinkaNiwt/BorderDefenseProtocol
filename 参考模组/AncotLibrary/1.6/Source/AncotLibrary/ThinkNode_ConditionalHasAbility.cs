using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalHasAbility : ThinkNode_Conditional
{
	public AbilityDef ability;

	public bool includeTemporary;

	protected override bool Satisfied(Pawn pawn)
	{
		return pawn.abilities?.GetAbility(ability, includeTemporary) != null;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		ThinkNode_ConditionalHasAbility thinkNode_ConditionalHasAbility = (ThinkNode_ConditionalHasAbility)base.DeepCopy(resolve);
		thinkNode_ConditionalHasAbility.ability = ability;
		return thinkNode_ConditionalHasAbility;
	}
}
