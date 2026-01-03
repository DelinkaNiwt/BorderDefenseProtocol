using RimWorld;
using Verse;

namespace AncotLibrary;

public class Verb_ChargeShootMultiple_FullCharge : Verb_ChargeShoot
{
	protected int bulletPerShot = 0;

	protected virtual int BulletPerShot => base.verbProps_Custom.bulletPerBurstShot;

	private bool FullEnergyShot => base.verbProps_Custom.fullChargeShot;

	public override void WarmupComplete()
	{
		if (currentTarget.Thing is Pawn { Downed: false, IsColonyMech: false } pawn && CasterIsPawn && CasterPawn.skills != null)
		{
			float num = (pawn.HostileTo(caster) ? 170f : 20f);
			float num2 = verbProps.AdjustedFullCycleTime(this, CasterPawn);
			CasterPawn.skills.Learn(SkillDefOf.Shooting, num * num2);
		}
		bulletPerShot = BulletPerShot;
		if (FullEnergyShot && base.compCharge.CanBeUsed)
		{
			bulletPerShot = base.compCharge.Charge;
		}
		Find.BattleLog.Add(new BattleLogEntry_RangedFire(caster, currentTarget.HasThing ? currentTarget.Thing : null, base.EquipmentSource?.def, Projectile, ShotsPerBurst > 1));
		state = VerbState.Bursting;
		TryCastNextBurstShot();
	}

	protected override bool TryCastShot()
	{
		Log.Message("");
		bool flag = base.TryCastShot();
		if (flag && CasterIsPawn)
		{
			CasterPawn.records.Increment(RecordDefOf.ShotsFired);
		}
		for (int i = 1; i < bulletPerShot; i++)
		{
			base.TryCastShot();
		}
		return flag;
	}

	public override void ExposeData()
	{
		Scribe_Values.Look(ref bulletPerShot, "bulletPerShot", 0);
		base.ExposeData();
	}
}
