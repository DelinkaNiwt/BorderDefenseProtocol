using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Verb_ShootMultiTarget : Verb_Shoot
{
	public float TargetAquireRange = 3.3f;

	protected List<Thing> TargetList = new List<Thing>();

	private VerbProp_SMT Props => (VerbProp_SMT)verbProps;

	protected override int ShotsPerBurst => verbProps.burstShotCount;

	public int ShootNum => Props.PPShootNum;

	protected override bool TryCastShot()
	{
		for (int i = 0; i < Props.PPShootNum; i++)
		{
			base.TryCastShot();
		}
		if (caster is Building_TurretGun || caster is Building_CMCTurretGun)
		{
			caster.TryGetComp<CompRefuelable>()?.ConsumeFuel(Props.PPShootNum);
		}
		return true;
	}

	private void ThrowDebugText(string text)
	{
		if (DebugViewSettings.drawShooting)
		{
			MoteMaker.ThrowText(caster.DrawPos, caster.Map, text);
		}
	}

	private void ThrowDebugText(string text, IntVec3 c)
	{
		if (DebugViewSettings.drawShooting)
		{
			MoteMaker.ThrowText(c.ToVector3Shifted(), caster.Map, text);
		}
	}
}
