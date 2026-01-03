using System;
using RimWorld;
using Verse;

namespace Milira;

public class CompSunBlastFurnaceOutdoorBreakdown : ThingComp
{
	private CompRefuelable refuelComp => parent.GetComp<CompRefuelable>();

	public void Notify_UsedThisTick()
	{
		float num = Rand.Range(0f, 100f);
		if (num < 1f)
		{
			DoBreakdown();
		}
	}

	public void DoBreakdown()
	{
		float radius = Math.Min(25f, refuelComp.Fuel / 2f);
		int damAmount = (int)refuelComp.Fuel;
		float fuel = refuelComp.Fuel;
		GenExplosion.DoExplosion(parent.Position, parent.Map, radius, DamageDefOf.Bomb, null, damAmount, fuel, null, null, null, null, ThingDefOf.Filth_Fuel, 0.6f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 0, 1f);
		refuelComp.ConsumeFuel(refuelComp.Fuel);
		parent.Destroy();
	}
}
