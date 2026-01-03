using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class Verb_Job : Verb
{
	public VerbProperties_Job verbProps_Job => (VerbProperties_Job)verbProps;

	protected override bool TryCastShot()
	{
		return TryGiveJob(base.EquipmentSource.TryGetComp<CompApparelVerbOwner_Charged>(), currentTarget);
	}

	public bool TryGiveJob(CompApparelVerbOwner_Charged reloadable, LocalTargetInfo target)
	{
		Pawn pawn = caster as Pawn;
		if (reloadable == null || !reloadable.CanBeUsed(out var _) || pawn == null || pawn.apparel.WornApparel.NullOrEmpty())
		{
			return false;
		}
		if (pawn.CurJobDef == verbProps_Job.jobDef)
		{
			return false;
		}
		Job job = JobMaker.MakeJob(verbProps_Job.jobDef, pawn, target);
		job.count = verbProps_Job.jobNum;
		pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		reloadable.UsedOnce();
		return true;
	}

	public override void OrderForceTarget(LocalTargetInfo target)
	{
		Job job = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStatic, target);
		job.verbToUse = this;
		CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
	}
}
