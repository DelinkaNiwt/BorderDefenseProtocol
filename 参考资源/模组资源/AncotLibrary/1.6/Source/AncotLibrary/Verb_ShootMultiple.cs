using RimWorld;
using Verse;

namespace AncotLibrary;

public class Verb_ShootMultiple : Verb_LaunchProjectile
{
	public VerbProperties_Custom verbProps_Custom => (VerbProperties_Custom)verbProps;

	protected virtual int BulletPerShot => verbProps_Custom.bulletPerBurstShot;

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
