using RimWorld;
using Verse;

namespace GD3
{
	public class Verb_ShootBackfire : Verb_LaunchProjectileBackfire
	{
		protected override int ShotsPerBurst => verbProps.burstShotCount;

		public override void WarmupComplete()
		{
			base.WarmupComplete();
			if (currentTarget.Thing is Pawn pawn && !pawn.Downed && !pawn.IsColonyMech && CasterIsPawn && CasterPawn.skills != null)
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
			bool num = base.TryCastShot();
			if (num && CasterIsPawn)
			{
				CasterPawn.records.Increment(RecordDefOf.ShotsFired);
			}
			return num;
		}
	}
}
