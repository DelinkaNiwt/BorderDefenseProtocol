using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GD3
{
	public class JobGiver_AIAbilityFightAndKeepDistance : JobGiver_AIFightEnemy
	{
		private float minDistance = 6.9f;

		private AbilityDef ability;

		protected override bool OnlyUseAbilityVerbs => true;

		protected override bool OnlyUseRangedSearch => true;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_AIAbilityFightAndKeepDistance obj = (JobGiver_AIAbilityFightAndKeepDistance)base.DeepCopy(resolve);
			obj.minDistance = minDistance;
			obj.ability = ability;
			return obj;
		}

		protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
		{
			dest = IntVec3.Invalid;
			Thing enemyTarget = pawn.mindState.enemyTarget;
			Ability ability = pawn.abilities.GetAbility(this.ability ?? GDDefOf.BlackApocriton_Thunder);
			CastPositionRequest newReq = default(CastPositionRequest);
			newReq.caster = pawn;
			newReq.target = enemyTarget;
			newReq.verb = ability.verb;
			newReq.maxRangeFromTarget = ability.verb.EffectiveRange;
			newReq.wantCoverFromTarget = false;
			newReq.preferredCastPosition = pawn.Position;
			newReq.validator = delegate (IntVec3 cell)
			{
				return cell.DistanceTo(pawn.Position) >= minDistance;
			};
			return CastPositionFinder.TryFindCastPosition(newReq, out dest);
		}
	}

}