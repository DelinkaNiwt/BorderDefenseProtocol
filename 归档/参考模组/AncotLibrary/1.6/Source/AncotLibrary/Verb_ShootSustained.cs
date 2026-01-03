using RimWorld;
using Verse;

namespace AncotLibrary;

public class Verb_ShootSustained : Verb_Shoot
{
	public Pawn forceTargetedDownedPawn;

	public VerbProperties_Custom VerbProps_Custom => verbProps as VerbProperties_Custom;

	public CompSustainedShoot CompSustainedShoot => base.EquipmentSource.TryGetComp<CompSustainedShoot>();

	protected override int ShotsPerBurst
	{
		get
		{
			if (state == VerbState.Idle && CompSustainedShoot.cachedBurstShotsLeft > 1)
			{
				return CompSustainedShoot.cachedBurstShotsLeft;
			}
			return base.BurstShotCount;
		}
	}

	public int BurstShotsLeft => burstShotsLeft;

	public override void OrderForceTarget(LocalTargetInfo target)
	{
		forceTargetedDownedPawn = null;
		base.OrderForceTarget(target);
		if (target.Pawn != null && target.Pawn.Downed && target.Pawn.Spawned)
		{
			forceTargetedDownedPawn = target.Pawn;
		}
		currentTarget = target;
	}

	public override void WarmupComplete()
	{
		burstShotsLeft = ShotsPerBurst;
		state = VerbState.Bursting;
		TryCastNextBurstShot();
		if (currentTarget.Thing is Pawn { Downed: false, IsColonyMech: false } pawn && CasterIsPawn && CasterPawn.skills != null && burstShotsLeft == base.BurstShotCount)
		{
			float num = (pawn.HostileTo(caster) ? 170f : 20f);
			float num2 = verbProps.AdjustedFullCycleTime(this, CasterPawn);
			CasterPawn.skills.Learn(SkillDefOf.Shooting, num * num2);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref forceTargetedDownedPawn, "forceTargetedDownedPawn");
	}
}
