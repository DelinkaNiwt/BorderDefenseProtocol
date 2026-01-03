using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalHasAbilityCategory : ThinkNode_Conditional
{
	public AbilityCategoryDef category;

	protected override bool Satisfied(Pawn pawn)
	{
		foreach (Ability item in pawn.abilities.AllAbilitiesForReading)
		{
			if (item.def.category == category)
			{
				return true;
			}
		}
		return false;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		ThinkNode_ConditionalHasAbilityCategory thinkNode_ConditionalHasAbilityCategory = (ThinkNode_ConditionalHasAbilityCategory)base.DeepCopy(resolve);
		thinkNode_ConditionalHasAbilityCategory.category = category;
		return thinkNode_ConditionalHasAbilityCategory;
	}
}
