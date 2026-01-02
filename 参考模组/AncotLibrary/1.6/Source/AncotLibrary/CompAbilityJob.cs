using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class CompAbilityJob : CompAbilityEffect
{
	public new CompProperties_AbilityJob Props => (CompProperties_AbilityJob)props;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Job job = JobMaker.MakeJob(Props.jobDef, target);
		job.count = 1;
		parent.pawn.jobs.StopAll();
		parent.pawn.jobs.StartJob(job, JobCondition.InterruptForced);
	}
}
