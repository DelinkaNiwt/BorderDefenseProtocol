using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalAbilityCooldownPct : ThinkNode_Conditional
{
	public AbilityDef ability;

	public float cooldownPct = 1f;

	protected override bool Satisfied(Pawn pawn)
	{
		if (ability == null)
		{
			return false;
		}
		float num = pawn.abilities.GetAbility(ability, includeTemporary: true).CooldownTicksRemaining;
		float num2 = pawn.abilities.GetAbility(ability, includeTemporary: true).CooldownTicksTotal;
		return num / num2 > cooldownPct;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		ThinkNode_ConditionalAbilityCooldownPct thinkNode_ConditionalAbilityCooldownPct = (ThinkNode_ConditionalAbilityCooldownPct)base.DeepCopy(resolve);
		thinkNode_ConditionalAbilityCooldownPct.ability = ability;
		return thinkNode_ConditionalAbilityCooldownPct;
	}
}
