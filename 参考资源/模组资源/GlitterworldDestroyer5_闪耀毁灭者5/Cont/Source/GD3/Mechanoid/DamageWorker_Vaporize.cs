using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GD3
{
	public class DamageWorker_Vaporize : DamageWorker_AddInjury
	{
		public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes)
		{
			bool flag = c.DistanceTo(explosion.Position) <= VaporizeRadius;
			c.GetFirstThing(explosion.Map, ThingDefOf.Filth_FireFoam)?.Destroy();
			base.ExplosionAffectCell(explosion, c, damagedThings, ignoredThings, canThrowMotes && flag);
			FireUtility.TryStartFireIn(c, explosion.Map, FireSizeRange.RandomInRange, explosion.instigator);
			if (flag)
			{
				FleckMaker.ThrowSmoke(c.ToVector3Shifted(), explosion.Map, 2f);
			}
		}

		protected override void ExplosionDamageThing(Explosion explosion, Thing t, List<Thing> damagedThings, List<Thing> ignoredThings, IntVec3 cell)
		{
			if (cell.DistanceTo(explosion.Position) <= VaporizeRadius)
			{
				base.ExplosionDamageThing(explosion, t, damagedThings, ignoredThings, cell);
			}
		}

		public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
		{
			base.ExplosionStart(explosion, cellsToAffect);
			Effecter effecter = EffecterDefOf.Vaporize_Heatwave.Spawn();
			effecter.Trigger(new TargetInfo(explosion.Position, explosion.Map, false), TargetInfo.Invalid, -1);
			effecter.Cleanup();
		}

		private static float VaporizeRadius = GDSettings.VaporizeRange;

		private static readonly FloatRange FireSizeRange = new FloatRange(0.4f, 0.8f);
	}
}