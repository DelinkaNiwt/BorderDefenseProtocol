using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class Verb_ShootSwitchFire : Verb_LauncherProjectileSwitchFire
{
	protected override int ShotsPerBurst => verbProps.burstShotCount;

	public override void WarmupComplete()
	{
		base.WarmupComplete();
		if (currentTarget.Thing is Pawn { Downed: false, IsColonyMech: false } pawn && CasterIsPawn && CasterPawn.skills != null)
		{
			float num = (pawn.HostileTo(caster) ? 170f : 20f);
			float num2 = verbProps.AdjustedFullCycleTime(this, CasterPawn);
			CasterPawn.skills.Learn(SkillDefOf.Shooting, num * num2);
		}
	}

	protected override bool TryCastShot()
	{
		Retarget();
		doRetarget = true;
		bool flag = base.TryCastShot();
		if (flag && CasterIsPawn)
		{
			CasterPawn.records.Increment(RecordDefOf.ShotsFired);
		}
		return flag;
	}
}
