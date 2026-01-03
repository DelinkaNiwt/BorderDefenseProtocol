using RimWorld;
using Verse;

namespace Milira;

public class Projectile_ExplosiveWithSmokeTrail : Projectile_Explosive
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
			int num = 2;
			if (GenTicks.TicksGame % num == 0)
			{
				FleckMaker.ThrowAirPuffUp(DrawPos, base.Map);
			}
		}
	}
}
