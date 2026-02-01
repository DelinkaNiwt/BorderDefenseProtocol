using RimWorld;
using Verse;
using Verse.AI;

namespace NCL;

public class Verb_FlyingKick : Verb
{
	public override bool MultiSelect => true;

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		return caster != null && CanHitTarget(target) && JumpUtility.ValidJumpTarget(caster, caster.Map, target.Cell);
	}

	public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
	{
		float num = EffectiveRange * EffectiveRange;
		IntVec3 cell = targ.Cell;
		return (float)caster.Position.DistanceToSquared(cell) <= num && GenSight.LineOfSight(root, cell, caster.Map, skipFirstCell: false, null, 0, 0);
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		base.DrawHighlight(target);
		GenDraw.DrawLineBetween(caster.TrueCenter(), target.CenterVector3);
	}

	public override void OnGUI(LocalTargetInfo target)
	{
		if (CanHitTarget(target) && JumpUtility.ValidJumpTarget(caster, caster.Map, target.Cell))
		{
			base.OnGUI(target);
		}
		else
		{
			GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
		}
	}

	public override void OrderForceTarget(LocalTargetInfo target)
	{
		Job job = JobMaker.MakeJob(JobDefOf.CastJump, target);
		job.verbToUse = this;
		CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
	}

	protected override bool TryCastShot()
	{
		if (CasterPawn == null || !CasterPawn.Spawned || CasterPawn.stances.FullBodyBusy)
		{
			return false;
		}
		Attack();
		return true;
	}

	public void Attack()
	{
		AcceptanceReport? acceptanceReport = (base.DirectOwner as Ability)?.CanCast;
		if ((acceptanceReport.HasValue ? new bool?(!acceptanceReport.GetValueOrDefault()) : ((bool?)null)) ?? true)
		{
			CasterPawn.jobs.StopAll();
			return;
		}
		Pawn casterPawn = CasterPawn;
		IntVec3 cell = currentTarget.Cell;
		Map map = casterPawn.Map;
		(base.DirectOwner as Ability).Activate(currentTarget, cell);
		FlyingKick flyingKick = FlyingKick.Make(ThingDef.Named("NCL_FlyingKick"), base.DirectOwner as Ability, casterPawn, cell);
		if (flyingKick != null)
		{
			GenSpawn.Spawn(flyingKick, cell, map);
		}
	}
}
