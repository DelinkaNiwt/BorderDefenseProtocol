using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Milira;

public static class MiliraFlyUtility_Rook
{
	public static IntVec3 TargetPosition(Pawn pawn, LocalTargetInfo currentTarget)
	{
		IntVec3 position = pawn.Position;
		IntVec3 cell = currentTarget.Cell;
		IntVec3 intVec = cell - position;
		IntVec3 result = position;
		Vector3 vector = intVec.ToVector3();
		vector.Normalize();
		float num = 42f;
		Map map = pawn.Map;
		for (int i = 0; (float)i < num; i++)
		{
			Vector3 vect = i * vector;
			IntVec3 intVec2 = position + vect.ToIntVec3();
			if (!ValidJumpTarget(map, intVec2))
			{
				break;
			}
			result = intVec2;
		}
		return result;
	}

	public static bool DoJump(Pawn pawn, LocalTargetInfo currentTarget, VerbProperties verbProps, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo), ThingDef pawnFlyerOverride = null)
	{
		IntVec3 position = pawn.Position;
		IntVec3 cell = currentTarget.Cell;
		Vector3 vector = (cell - position).ToVector3();
		vector.Normalize();
		IntVec3 intVec = TargetPosition(pawn, currentTarget);
		Map map = pawn.Map;
		bool flag = Find.Selector.IsSelected(pawn);
		MiliraPawnFlyer_Rook miliraPawnFlyer_Rook = MiliraPawnFlyer_Rook.MakeFlyer(pawnFlyerOverride ?? MiliraDefOf.Milira_PawnJumper_Rook, pawn, intVec, verbProps.flightEffecterDef, verbProps.soundLanding, verbProps.flyWithCarriedThing, null, triggeringAbility, target);
		if (miliraPawnFlyer_Rook != null)
		{
			FleckMaker.ThrowDustPuff(position.ToVector3Shifted() - vector, map, 2f);
			GenSpawn.Spawn(miliraPawnFlyer_Rook, intVec, map);
			if (flag)
			{
				Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
			}
			return true;
		}
		return false;
	}

	public static void OrderJump(Pawn pawn, LocalTargetInfo target, Verb verb, float range)
	{
		IntVec3 cell = target.Cell;
		IntVec3 root = TargetPosition(pawn, target);
		Map map = pawn.Map;
		IntVec3 intVec = RCellFinder.BestOrderedGotoDestNear(root, pawn, (IntVec3 c) => ValidJumpTarget(map, c) && CanHitTargetFrom(pawn, pawn.Position, c, range));
		Job job = JobMaker.MakeJob(JobDefOf.CastJump, cell);
		job.verbToUse = verb;
		if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
		{
			FleckMaker.Static(cell, map, RimWorld.FleckDefOf.FeedbackGoto);
		}
	}

	public static bool CanHitTargetFrom(Pawn pawn, IntVec3 root, LocalTargetInfo targ, float range)
	{
		float num = range * range;
		IntVec3 cell = targ.Cell;
		if ((float)pawn.Position.DistanceToSquared(cell) <= num)
		{
			return GenSight.LineOfSight(root, cell, pawn.Map);
		}
		return false;
	}

	public static bool ValidJumpTarget(Map map, IntVec3 cell)
	{
		if (!cell.IsValid || !cell.InBounds(map))
		{
			return false;
		}
		if (cell.Impassable(map) || !cell.Walkable(map) || cell.Fogged(map))
		{
			return false;
		}
		Building edifice = cell.GetEdifice(map);
		if (edifice != null && edifice is Building_Door { Open: false })
		{
			return false;
		}
		return true;
	}
}
