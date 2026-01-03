using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class ThinkNode_ConditionalHasEnoughHungerRestForAbility : ThinkNode_Conditional
{
	public AbilityDef abilityDef;

	protected override bool Satisfied(Pawn pawn)
	{
		return GetHungerRestCostEffect(pawn)?.AICanTargetNow(LocalTargetInfo.Invalid) ?? true;
	}

	private CompAbilityEffect_HungerRestCost GetHungerRestCostEffect(Pawn pawn)
	{
		Ability ability = pawn.abilities?.GetAbility(abilityDef, includeTemporary: true);
		if (ability == null)
		{
			return null;
		}
		foreach (CompAbilityEffect effectComp in ability.EffectComps)
		{
			if (effectComp is CompAbilityEffect_HungerRestCost result)
			{
				return result;
			}
		}
		return null;
	}
}
