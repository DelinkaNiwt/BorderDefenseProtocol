using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NCLWorm;

public class JobGiver_UseNCLWormSkillList : ThinkNode_JobGiver
{
	private readonly List<AbilityDef> tmpAttackss = new List<AbilityDef>();

	private static readonly List<Thing> targetsTmp = new List<Thing>();

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (IsAnyAttackOnCooldown(pawn))
		{
			return null;
		}
		Thing thing = FindTarget(pawn);
		if (thing == null)
		{
			return null;
		}
		return GetRandomCanUseAbility(pawn, thing)?.GetJob(thing, null);
	}

	private static bool IsPawnTarget(Pawn pawn, Thing thing)
	{
		if (thing is Pawn { Dead: false, Downed: false } pawn2 && pawn.Position.InHorDistOf(pawn2.Position, 50f) && pawn.CanSee(pawn2))
		{
			if (pawn2.Faction != null && pawn.Faction != null)
			{
				return pawn2.Faction.HostileTo(pawn.Faction);
			}
			return pawn2.HostileTo(pawn);
		}
		return false;
	}

	public static Thing FindTarget(Pawn pawn)
	{
		List<Thing> source = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn);
		CheckForTargets(pawn, source, targetsTmp, IsPawnTarget);
		Thing result = null;
		if (!targetsTmp.Empty())
		{
			result = targetsTmp.RandomElement();
		}
		targetsTmp.Clear();
		return result;
	}

	private static void CheckForTargets(Pawn pawn, List<Thing> source, List<Thing> output, Func<Pawn, Thing, bool> validator)
	{
		output.Clear();
		for (int i = 0; i < source.Count; i++)
		{
			if (validator(pawn, source[i]))
			{
				output.Add(source[i]);
			}
		}
	}

	private bool IsAnyAttackOnCooldown(Pawn pawn)
	{
		List<AbilityDef> skill = pawn.TryGetComp<Comp_NCLWormSkillList>().Props.skill;
		for (int i = 0; i < skill.Count; i++)
		{
			Ability ability = pawn.abilities.GetAbility(skill[i]);
			if (!ability.CanCast)
			{
				return true;
			}
		}
		return false;
	}

	private Ability GetRandomCanUseAbility(Pawn pawn, Thing target)
	{
		List<AbilityDef> skill = pawn.TryGetComp<Comp_NCLWormSkillList>().Props.skill;
		for (int i = 0; i < skill.Count; i++)
		{
			if ((bool)pawn.abilities.GetAbility(skill[i]).CanCast)
			{
				tmpAttackss.Add(skill[i]);
			}
		}
		if (tmpAttackss.Empty())
		{
			return null;
		}
		return pawn.abilities.GetAbility(tmpAttackss.RandomElement());
	}
}
