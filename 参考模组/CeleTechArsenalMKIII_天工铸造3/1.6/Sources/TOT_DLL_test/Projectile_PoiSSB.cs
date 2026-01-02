using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Projectile_PoiSSB : Projectile_PoiBullet
{
	private bool flag3 = true;

	private bool CalHit = false;

	private int tickcount = 0;

	public override bool CanHitTarget()
	{
		bool flag = Rand.Chance(0.88f);
		if (!flag)
		{
			destination = intendedTarget.CenterVector3 + new Vector3(Rand.Range(-0.5f, 0.5f), 0f, Rand.Range(-0.5f, 0.5f));
		}
		return flag;
	}

	protected override void Tick()
	{
		tickcount++;
		Fleck_MakeFleckTick++;
		if (Fleck_MakeFleckTick >= Fleck_MakeFleckTickMax)
		{
			Vector3 vector = BPos(base.DistanceCoveredFraction);
			Map map = base.Map;
			if (tickcount <= 1)
			{
				FleckMaker.ThrowLightningGlow(origin, map, 0.33f);
			}
			if (tickcount <= 5)
			{
				FleckMaker.ThrowBreathPuff(vector, map, (lastposition - vector).ToAngleFlat(), new Vector3(0.15f, 0f, 0.3f));
			}
		}
		if (flag3)
		{
			CalHit = CanHitTarget();
			flag3 = false;
		}
		if (intendedTarget.Thing != null && CalHit && intendedTarget.Thing is Pawn { DeadOrDowned: false })
		{
			destination = intendedTarget.Thing.DrawPos;
		}
		base.Tick();
	}
}
