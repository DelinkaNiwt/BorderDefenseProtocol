using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class CompTargetEffect_EquipMilian : CompTargetEffect
{
	public CompProperties_TargetEffect_EquipMilian Props => (CompProperties_TargetEffect_EquipMilian)props;

	public override void DoEffectOn(Pawn user, Thing target)
	{
		if (user.IsColonistPlayerControlled && user.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly))
		{
			Job job = JobMaker.MakeJob(MiliraDefOf.Milira_EquipMilian, parent, target);
			job.count = 1;
			user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}
	}
}
