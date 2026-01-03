using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public abstract class JobGiver_AICastAbilityCategory : ThinkNode_JobGiver
{
	protected AbilityCategoryDef category;

	protected Ability Ability(Pawn pawn)
	{
		List<Ability> list = new List<Ability>();
		foreach (Ability item in pawn.abilities.AllAbilitiesForReading)
		{
			if (item.def.category == category && (bool)item.CanCast)
			{
				list.Add(item);
			}
		}
		if (list.Count > 0)
		{
			return list.RandomElement();
		}
		return null;
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.CurJob?.ability?.def.category == category)
		{
			return null;
		}
		Ability ability = Ability(pawn);
		if (ability == null || !ability.CanCast)
		{
			return null;
		}
		LocalTargetInfo target = GetTarget(pawn, ability);
		if (!target.IsValid)
		{
			return null;
		}
		return ability.GetJob(target, target);
	}

	protected abstract LocalTargetInfo GetTarget(Pawn caster, Ability ability);

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_AICastAbilityCategory jobGiver_AICastAbilityCategory = (JobGiver_AICastAbilityCategory)base.DeepCopy(resolve);
		jobGiver_AICastAbilityCategory.category = category;
		return jobGiver_AICastAbilityCategory;
	}
}
