using RimWorld;
using Verse;
using Verse.AI;

namespace NyarsModPackOne;

public class JobGiver_FollowTarget : ThinkNode_JobGiver
{
	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn is Drone { owner: not null } drone)
		{
			if (!drone.owner.Spawned || (float)(drone.owner.Position - pawn.Position).LengthHorizontalSquared < 2.25f)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf.FollowClose, drone.owner);
			job.expiryInterval = 240;
			job.checkOverrideOnExpire = true;
			job.followRadius = 1.5f;
			return job;
		}
		return null;
	}
}
