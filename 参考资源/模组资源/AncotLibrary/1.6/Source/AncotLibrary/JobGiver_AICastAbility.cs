using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public abstract class JobGiver_AICastAbility : ThinkNode_JobGiver
{
	protected AbilityDef ability;

	public bool includeTemporary;

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.CurJob?.ability?.def == this.ability)
		{
			return null;
		}
		Ability ability = pawn.abilities?.GetAbility(this.ability, includeTemporary);
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
		JobGiver_AICastAbility jobGiver_AICastAbility = (JobGiver_AICastAbility)base.DeepCopy(resolve);
		jobGiver_AICastAbility.ability = ability;
		return jobGiver_AICastAbility;
	}
}
