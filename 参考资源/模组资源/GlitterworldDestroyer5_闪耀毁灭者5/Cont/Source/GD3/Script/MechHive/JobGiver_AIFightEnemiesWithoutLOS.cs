using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GD3
{
	public class JobGiver_AIFightEnemiesWithoutLOS : JobGiver_AIFightEnemies
	{
		protected override Thing FindAttackTarget(Pawn pawn)
		{
			float num = float.MaxValue;
			Thing thing = null;
			List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
			for (int i = 0; i < potentialTargetsFor.Count; i++)
			{
				IAttackTarget attackTarget = potentialTargetsFor[i];
				if (!attackTarget.ThreatDisabled(pawn) && AttackTargetFinder.IsAutoTargetable(attackTarget) && (!humanlikesOnly || !(attackTarget is Pawn pawn2) || pawn2.RaceProps.Humanlike) && (!(attackTarget.Thing is Pawn pawn3) || pawn3.IsCombatant() || !ignoreNonCombatants) && (pawn.Faction == null || !pawn.Faction.IsPlayer || !attackTarget.Thing.Position.Fogged(pawn.Map)))
				{
					Thing thing2 = (Thing)attackTarget;
					int num2 = thing2.Position.DistanceToSquared(pawn.Position);
					if ((float)num2 < num && pawn.CanReach(thing2, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.PassDoors))
					{
						Log.Message(2);
						num = num2;
						thing = thing2;
					}
				}
			}
			return thing;
		}
	}

}
