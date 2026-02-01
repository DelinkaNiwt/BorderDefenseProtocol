using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_ForceJobOnTargetBase : AbilityExtension_AbilityMod
{
	public JobDef jobDef;

	public StatDef durationMultiplier;

	public FleckDef fleckOnTarget;

	protected void ForceJob(GlobalTargetInfo target, Ability ability)
	{
		if (target.Thing is Pawn pawn)
		{
			Job job = JobMaker.MakeJob(jobDef, ability.pawn);
			float num = 1f;
			if (durationMultiplier != null)
			{
				num = pawn.GetStatValue(durationMultiplier);
			}
			job.expiryInterval = (int)((float)ability.GetDurationForPawn() * num);
			job.mote = MoteMaker.MakeThoughtBubble(pawn, ability.def.iconPath, maintain: true);
			pawn.jobs.StopAll();
			pawn.jobs.StartJob(job, JobCondition.InterruptForced);
			if (fleckOnTarget != null)
			{
				Ability.MakeStaticFleck(pawn.DrawPos, pawn.Map, fleckOnTarget, 1f, 0f);
			}
		}
	}
}
