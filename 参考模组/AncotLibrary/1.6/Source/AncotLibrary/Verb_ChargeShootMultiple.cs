using RimWorld;

namespace AncotLibrary;

public class Verb_ChargeShootMultiple : Verb_ChargeShoot
{
	protected virtual int BulletPerShot => base.verbProps_Custom.bulletPerBurstShot;

	protected override bool TryCastShot()
	{
		bool flag = base.TryCastShot();
		if (flag && CasterIsPawn)
		{
			CasterPawn.records.Increment(RecordDefOf.ShotsFired);
		}
		for (int i = 1; i < BulletPerShot; i++)
		{
			base.TryCastShot();
		}
		return flag;
	}
}
