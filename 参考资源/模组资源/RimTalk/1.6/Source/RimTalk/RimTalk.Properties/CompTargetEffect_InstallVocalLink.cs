using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalk.Properties;

public class CompTargetEffect_InstallVocalLink : CompTargetEffect
{
	public CompProperties_TargetEffectInstallVocalLink Props => (CompProperties_TargetEffectInstallVocalLink)props;

	public override void DoEffectOn(Pawn user, Thing target)
	{
		if (user.IsColonistPlayerControlled)
		{
			Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ApplyVocalLinkCatalyst"), target, parent);
			job.count = 1;
			job.playerForced = true;
			user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}
	}
}
