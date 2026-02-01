using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
	public class HediffCompProperties_OverHorizon : HediffCompProperties
	{
		public HediffCompProperties_OverHorizon()
		{
			this.compClass = typeof(HediffComp_OverHorizon);
		}

		public ThingDef missile;
	}

	public class HediffComp_OverHorizon : HediffComp
	{
		public HediffCompProperties_OverHorizon Props
		{
			get
			{
				return (HediffCompProperties_OverHorizon)this.props;
			}
		}

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
			this.ticks++;
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
			base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
			if (dinfo.Def == DamageDefOf.EMP)
            {
				return;
            }
			if (totalDamageDealt == 0)
			{
				return;
			}
			if (this.ticks < 60)
            {
				return;
            }
			Pawn pawn = this.Pawn;
			ThingWithComps weapon = pawn.equipment.Primary;
			if (weapon == null || weapon.def.Verbs == null || weapon.def.Verbs[0].range <= 0)
            {
				return;
            }
			if (dinfo.Instigator != null && dinfo.Instigator.Map == pawn.Map && dinfo.Instigator.Position.DistanceTo(pawn.Position) > weapon.def.Verbs[0].range)
            {
				//原先是陨石类导弹逻辑，现改为高角炮弹
				//StartRandomFire(dinfo.Instigator);
				//if (nextExplosionCell == IntVec3.Invalid)
				//{
				//	return;
				//}
				//GenPlace.TryPlaceThing(ThingMaker.MakeThing(this.Props.missile, null), this.nextExplosionCell, dinfo.Instigator.Map, ThingPlaceMode.Near, null, null, default(Rot4));
				Projectile projectile = (Projectile)GenSpawn.Spawn(Props.missile, pawn.PositionHeld, pawn.MapHeld);
				projectile.Launch(pawn, pawn.DrawPos, dinfo.Instigator.PositionHeld + GenRadial.RadialPattern[Rand.Range(0, 1)], dinfo.Instigator, ProjectileHitFlags.All, false, null);
				GDDefOf.Mortar_LaunchA.PlayOneShot(Pawn);
				this.ticks = 0;
			}
		}

		/*private void StartRandomFire(Thing victim)
		{
			nextExplosionCell = (from x in GenRadial.RadialCellsAround(victim.Position, impactAreaRadius, useCenter: true)
								 where x.InBounds(victim.MapHeld)
								 select x).RandomElementByWeight((IntVec3 x) => DistanceChanceFactor.Evaluate(x.DistanceTo(victim.PositionHeld) / impactAreaRadius));
		}

		private float impactAreaRadius = 2.9f;

		private IntVec3 nextExplosionCell = IntVec3.Invalid;

		public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
		{
			new CurvePoint(0f, 1f),
			new CurvePoint(1f, 0.1f)
		};*/

		private int ticks = 0;
	}
}
