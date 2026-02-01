using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GD3
{
	public class JobGiver_TrySupportShield : ThinkNode_JobGiver
	{
		protected Job Shield(Pawn user)
		{
			Ability ability = user.abilities?.GetAbility(GDDefOf.GD_BlackShieldSupport, false);
			if (ability == null || !ability.CanCast || ability.Casting || user.Faction == null)
			{
				return null;
			}

			List<Pawn> pawns = user.Map.mapPawns.AllPawns.FindAll((Pawn p) => p != user && p.Faction != null && p.Faction == user.Faction);
			if (pawns.Count > 0)
            {
				pawns.SortBy((Pawn p) => p.Position.DistanceTo(user.Position));
			}
			List<Pawn> pawnsEnemy = user.Map.mapPawns.AllPawns.FindAll((Pawn p) => p.Faction != null && p.Faction.HostileTo(user.Faction));
			if (pawnsEnemy.Count > 0)
            {
				pawnsEnemy.SortBy((Pawn p) => p.Position.DistanceTo(user.Position));
			}
			Pawn pawn = user;
			if (pawns.Count > 0)
            {
				pawn = pawns[0];
            }
			if (pawnsEnemy.Count > 0 && pawnsEnemy[0].Position.DistanceTo(user.Position) < 7.9f)
            {
				pawn = user;
            }

			if (pawn == null)
            {
				Log.Error("Shield ability get no pawn.");
				return null;
            }

			Job job = JobMaker.MakeJob(GDDefOf.CastAbilityGoToThing);
			job.ability = ability;
			job.targetA = pawn;
			if (pawn != user)
            {
				CastPositionRequest newReq = default(CastPositionRequest);
				newReq.caster = user;
				newReq.target = pawn;
				newReq.verb = ability.verb;
				newReq.maxRangeFromTarget = ability.verb.verbProps.range - 3f;
				newReq.locus = (IntVec3)pawn.mindState.duty.focus;
				newReq.maxRangeFromLocus = 9999f;
				CastPositionFinder.TryFindCastPosition(newReq, out IntVec3 dest);
				job.targetB = dest;
            }
            else
            {
				job.targetB = user.Position;
            }
			job.verbToUse = ability.verb;
			return job;
		}
		protected override Job TryGiveJob(Pawn user)
		{
			return this.Shield(user);
		}
	}
}
