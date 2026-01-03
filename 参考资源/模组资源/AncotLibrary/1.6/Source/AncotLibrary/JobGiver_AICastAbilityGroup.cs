using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public abstract class JobGiver_AICastAbilityGroup : ThinkNode_JobGiver
{
	protected List<AbilityDef> abilities;

	protected Ability Ability(Pawn pawn)
	{
		List<Ability> list = new List<Ability>();
		foreach (Ability item in pawn.abilities.AllAbilitiesForReading)
		{
			if (abilities.Contains(item.def) && (bool)item.CanCast)
			{
				list.Add(item);
			}
		}
		return list.RandomElement();
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (abilities.Contains(pawn.CurJob?.ability?.def))
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
		JobGiver_AICastAbilityGroup jobGiver_AICastAbilityGroup = (JobGiver_AICastAbilityGroup)base.DeepCopy(resolve);
		jobGiver_AICastAbilityGroup.abilities = abilities;
		return jobGiver_AICastAbilityGroup;
	}
}
