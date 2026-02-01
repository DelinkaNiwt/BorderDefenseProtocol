using RimWorld;
using Verse;
using Verse.AI;

namespace NCLWorm;

public class JobGiver_GetBeacon : ThinkNode_JobGiver
{
	public static Building GetBeacon(Pawn pawn)
	{
		return (Building)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("NCLCommsConsole")), PathEndMode.InteractionCell, TraverseParms.For(pawn), 9999f, delegate(Thing t)
		{
			Building building = (Building)t;
			return pawn.CanReach(t, PathEndMode.InteractionCell, Danger.Deadly) ? true : false;
		});
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		IntVec3 interactionCell = GetBeacon(pawn).InteractionCell;
		if (interactionCell.IsValid && !pawn.Position.InHorDistOf(interactionCell, 3f))
		{
			return JobMaker.MakeJob(JobDefOf.Goto, interactionCell);
		}
		return null;
	}
}
