using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded;

public class JobGiver_Flee : ThinkNode_JobGiver
{
	protected override Job TryGiveJob(Pawn pawn)
	{
		List<Pawn> list = (from x in pawn.Map.mapPawns.AllPawnsSpawned
			where !x.Dead && !x.Downed && x.Position.DistanceTo(pawn.Position) < 50f && GenSight.LineOfSight(x.Position, pawn.Position, pawn.Map)
			orderby x.Position.DistanceTo(pawn.Position)
			select x).ToList();
		if (list.Any())
		{
			if (pawn.Faction != Faction.OfPlayer && CellFinderLoose.GetFleeExitPosition(pawn, 10f, out var position))
			{
				Job job = JobMaker.MakeJob(JobDefOf.Flee, position, list.First());
				job.exitMapOnArrival = true;
				return job;
			}
			return FleeJob(pawn, list.First(), list.Cast<Thing>().ToList());
		}
		return null;
	}

	public Job FleeJob(Pawn pawn, Thing danger, List<Thing> dangers)
	{
		Job result = null;
		IntVec3 intVec = ((pawn.CurJob == null || pawn.CurJob.def != JobDefOf.Flee) ? CellFinderLoose.GetFleeDest(pawn, dangers, 24f) : pawn.CurJob.targetA.Cell);
		if (intVec == pawn.Position)
		{
			intVec = GenRadial.RadialCellsAround(pawn.Position, 1f, 15f).RandomElement();
		}
		if (intVec != pawn.Position)
		{
			result = JobMaker.MakeJob(JobDefOf.Flee, intVec, danger);
		}
		return result;
	}
}
