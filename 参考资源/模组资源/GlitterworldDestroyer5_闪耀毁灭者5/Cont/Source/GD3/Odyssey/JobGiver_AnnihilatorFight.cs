using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace GD3
{
	public class JobGiver_AnnihilatorFight : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			Thing thing = pawn.mindState.enemyTarget;
			if (thing == null)
			{
				return null;
			}
			Annihilator annihilator = pawn as Annihilator;
			if (annihilator == null || annihilator.NoAI || annihilator.Dying)
            {
				return null;
            }
			return GetRandomPossibleAbility(pawn, thing)?.GetJob(thing, null);
		}

		private Ability GetRandomPossibleAbility(Pawn pawn, Thing target)
		{
			List<Ability> abilities = (pawn as Annihilator)?.abilitiesList;
			List<Ability> selectedAbilities = new List<Ability>();
			if (abilities.NullOrEmpty())
            {
				return null;
            }
			for (int i = 0; i < abilities.Count; i++)
			{
				Ability ability = abilities[i];
				Verb verb = ability.verb;
				if ((bool)ability.CanCast && (!ability.def.targetRequired || verb.CanHitTarget(target)) && ability.AICanTargetNow(target))
				{
					selectedAbilities.Add(ability);
				}
			}
			if (selectedAbilities.Empty())
			{
				return null;
			}
			return selectedAbilities.RandomElement();
		}
	}
}
