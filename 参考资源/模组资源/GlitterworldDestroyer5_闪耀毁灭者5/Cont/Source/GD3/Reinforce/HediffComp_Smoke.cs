using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
	public class HediffCompProperties_Smoke : HediffCompProperties
	{
		public HediffCompProperties_Smoke()
		{
			this.compClass = typeof(HediffComp_Smoke);
		}

		public float range;

		public int interval;
	}

	public class HediffComp_Smoke : HediffComp
	{
		public HediffCompProperties_Smoke Props
		{
			get
			{
				return (HediffCompProperties_Smoke)this.props;
			}
		}

		public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
			base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
			if (totalDamageDealt == 0)
			{
				return;
			}
			Pawn pawn = this.Pawn;
			bool flag = pawn != null && pawn.Spawned && !pawn.Downed && this.ticks <= 0;
			if (flag)
            {
				this.ticks = this.Props.interval;
				GenExplosion.DoExplosion(pawn.Position, pawn.Map, this.Props.range, DamageDefOf.Smoke, null, -1, -1f, null, null, null, null, null, 0f, 1, new GasType?(GasType.BlindSmoke), null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
			}
		}

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
			if (this.ticks <= 0)
            {
				return;
            }
			this.ticks--;
        }

        private int ticks = 0;
	}
}
