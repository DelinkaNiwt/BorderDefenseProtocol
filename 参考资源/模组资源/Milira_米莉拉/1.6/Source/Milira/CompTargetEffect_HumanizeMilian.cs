using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class CompTargetEffect_HumanizeMilian : CompTargetEffect
{
	public CompProperties_TargetEffect_HumanizeMilian Props => (CompProperties_TargetEffect_HumanizeMilian)props;

	public override void DoEffectOn(Pawn user, Thing target)
	{
		if (user.IsColonistPlayerControlled && user.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly))
		{
			Job job = JobMaker.MakeJob(MiliraDefOf.Milira_HumanizeMilian, target, parent);
			job.count = 1;
			user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}
	}
}
