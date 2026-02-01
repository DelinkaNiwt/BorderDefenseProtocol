using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace GD3
{
	public class JobGiver_AIGotoNearestHostileWhenFlying : ThinkNode_JobGiver
	{
		private bool ignoreNonCombatants;

		private bool humanlikesOnly;

		private int overrideExpiryInterval = -1;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_AIGotoNearestHostileWhenFlying obj = (JobGiver_AIGotoNearestHostileWhenFlying)base.DeepCopy(resolve);
			obj.ignoreNonCombatants = ignoreNonCombatants;
			obj.humanlikesOnly = humanlikesOnly;
			obj.overrideExpiryInterval = overrideExpiryInterval;
			return obj;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			float num = float.MaxValue;
			Thing thing = null;
			List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
			for (int i = 0; i < potentialTargetsFor.Count; i++)
			{
				IAttackTarget attackTarget = potentialTargetsFor[i];
				if (!attackTarget.ThreatDisabled(pawn) && AttackTargetFinder.IsAutoTargetable(attackTarget)
					&& (!humanlikesOnly || !(attackTarget is Pawn pawn2) || pawn2.RaceProps.Humanlike)
					&& (!(attackTarget.Thing is Pawn pawn3) || pawn3.IsCombatant() || (!ignoreNonCombatants && GenSight.LineOfSightToThing(pawn.Position, pawn3, pawn.Map)))
					&& (pawn.Faction == null || !pawn.Faction.IsPlayer || !attackTarget.Thing.Position.Fogged(pawn.Map)))
				{
					Thing thing2 = (Thing)attackTarget;
					int num2 = thing2.Position.DistanceToSquared(pawn.Position);
					if ((float)num2 < num && pawn.CanReach(thing2, PathEndMode.OnCell, Danger.Deadly))
					{
						num = num2;
						thing = thing2;
					}
				}
			}
			pawn.mindState.enemyTarget = thing;

			if (!pawn.Flying || (int)typeof(Pawn_FlightTracker).GetField("lerpTick", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pawn.flight) != 0)
            {
				return null;
            }
			
			if (thing != null)
			{
				if (thing.PositionHeld == pawn.PositionHeld || pawn.CanReachImmediate(thing, PathEndMode.Touch))
				{
					return null;
				}
				Job job = JobMaker.MakeJob(JobDefOf.Goto, thing);
				if (overrideExpiryInterval > 0)
				{
					job.expiryInterval = overrideExpiryInterval;
				}
				else
				{
					job.intervalScalingTarget = TargetIndex.A;
				}
				job.checkOverrideOnExpire = true;
				job.expireRequiresEnemiesNearby = true;
				job.collideWithPawns = false;
				return job;
			}
			return null;
		}
	}
}
