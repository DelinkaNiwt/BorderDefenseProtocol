using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class CompTargetEffect_DressMilian : CompTargetEffect
{
	public CompProperties_TargetEffect_DressMilian Props => (CompProperties_TargetEffect_DressMilian)props;

	public override void DoEffectOn(Pawn user, Thing target)
	{
		if (user.IsColonistPlayerControlled && user.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly))
		{
			Job job = JobMaker.MakeJob(MiliraDefOf.Milira_DressMilian, target, parent);
			job.count = 1;
			user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}
	}
}
