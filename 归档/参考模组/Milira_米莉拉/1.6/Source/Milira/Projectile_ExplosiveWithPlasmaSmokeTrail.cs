using UnityEngine;
using Verse;

namespace Milira;

public class Projectile_ExplosiveWithPlasmaSmokeTrail : Projectile_Explosive
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
				MiliraFleckMaker.ThrowPlasmaAirPuffUp(color: new Color(0.6f, 0.8f, 1f), loc: DrawPos, map: base.Map);
			}
		}
	}
}
