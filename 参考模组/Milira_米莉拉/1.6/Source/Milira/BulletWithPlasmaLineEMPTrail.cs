using RimWorld;
using Verse;

namespace Milira;

public class BulletWithPlasmaLineEMPTrail : Bullet
{
	protected override void Tick()
	{
		base.Tick();
		LeaveSmokeTrail();
	}

	private void LeaveSmokeTrail()
	{
		if (base.Map != null)
		{
			int num = 1;
			if (GenTicks.TicksGame % num == 0)
			{
				MiliraFleckMaker.ThrowLineEMP(DrawPos, base.Map);
			}
		}
	}
}
