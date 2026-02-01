using RimWorld;
using Verse;
using Verse.AI;

namespace NCL;

public class Verb_Charge : Verb
{
	public override bool MultiSelect => true;

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		return caster != null && CanHitTarget(target) && JumpUtility.ValidJumpTarget(caster, caster.Map, target.Cell);
	}

	public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
	{
		float rangeSqr = EffectiveRange * EffectiveRange;
		return (float)caster.Position.DistanceToSquared(targ.Cell) <= rangeSqr && GenSight.LineOfSight(root, targ.Cell, caster.Map, skipFirstCell: false, null, 0, 0);
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
		IntVec3 spawnPosition = CasterPawn.Position;
		Map map = CasterPawn.Map;
		LocalTargetInfo target = currentTarget;
		CreateChargeEffect(spawnPosition, map, target);
		return true;
	}

	private void CreateChargeEffect(IntVec3 spawnPosition, Map map, LocalTargetInfo target)
	{
		if (!(base.DirectOwner is Ability ability) || !ability.CanCast)
		{
			CasterPawn.jobs.StopAll();
			return;
		}
		ability.Activate(target, target.Cell);
		Charge charge = Charge.Make(ThingDef.Named("NCL_Charge"), ability, CasterPawn, target.Cell, null, null, flyWithCarriedThing: false);
		if (charge != null)
		{
			GenSpawn.Spawn(charge, spawnPosition, map);
			charge.SetBehaviorMode("ChargeA");
		}
	}
}
