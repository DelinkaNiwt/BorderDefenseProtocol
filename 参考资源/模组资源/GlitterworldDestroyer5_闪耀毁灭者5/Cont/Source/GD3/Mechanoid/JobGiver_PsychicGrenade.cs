using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GD3
{
	public class JobGiver_PsychicGrenade : ThinkNode_JobGiver
	{
		public static readonly IntRange ExpiryInterval_ShooterSucceeded = new IntRange(10, 30);

		private bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
		{
			Thing enemyTarget = pawn.mindState.enemyTarget;
			Verb verb = verbToUse ?? pawn.TryGetAttackVerb(enemyTarget, !pawn.IsColonist);
			if (verb == null)
			{
				dest = IntVec3.Invalid;
				return false;
			}
			CastPositionRequest newReq = default(CastPositionRequest);
			newReq.caster = pawn;
			newReq.target = enemyTarget;
			newReq.verb = verb;
			newReq.maxRangeFromTarget = 9999f;
			newReq.locus = (IntVec3)pawn.mindState.duty.focus;
			newReq.maxRangeFromLocus = pawn.mindState.duty.radius;
			newReq.wantCoverFromTarget = verb.verbProps.range > 7f;
			return CastPositionFinder.TryFindCastPosition(newReq, out dest);
		}
		protected Job ThrowGrenade(Pawn user)
		{
			Ability ability = user.abilities?.GetAbility(GDDefOf.ThrowPsychicGrenade, false);
			if (ability == null || !ability.CanCast || ability.Casting)
			{
				return null;
			}
			Pawn target = user.mindState.enemyTarget as Pawn;
			if (target != null && target.GetStatValue(StatDefOf.PsychicSensitivity) > 0 && target.Position.DistanceTo(user.Position) <= ability.VerbProperties[0].range)
            {
				if (!TryFindShootingPosition(user, out var dest2))
				{
					return null;
				}
				if (dest2 == user.Position)
				{
					Job job = JobMaker.MakeJob(JobDefOf.CastAbilityOnThing);
					job.ability = ability;
					job.targetA = target;
					return job;
				}
				Job job2 = JobMaker.MakeJob(JobDefOf.Goto, dest2);
				job2.expiryInterval = ExpiryInterval_ShooterSucceeded.RandomInRange;
				job2.checkOverrideOnExpire = true;
				return job2;
			}
			return null;
		}
		protected override Job TryGiveJob(Pawn user)
		{
			Random random = new Random();
			int i = random.Next(0, 99);
			if (i > 3)
			{
				return null;
			}
			return this.ThrowGrenade(user);
		}
	}
}
