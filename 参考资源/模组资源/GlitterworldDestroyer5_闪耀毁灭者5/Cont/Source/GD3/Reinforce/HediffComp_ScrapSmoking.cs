using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
	public class HediffCompProperties_ScrapSmoking : HediffCompProperties
	{
		public HediffCompProperties_ScrapSmoking()
		{
			this.compClass = typeof(HediffComp_ScrapSmoking);
		}
	}

	public class HediffComp_ScrapSmoking : HediffComp
	{
		public HediffCompProperties_ScrapSmoking Props
		{
			get
			{
				return (HediffCompProperties_ScrapSmoking)this.props;
			}
		}

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			this.ticks++;
			if (this.ticks > 2)
			{
				this.ticks = 0;
				Pawn p = this.Pawn;
				if (p != null)
                {
					FleckMaker.ThrowSmoke(p.DrawPos, p.Map, 1.6f);
                }
			}
		}

		private int ticks = 0;
	}
}
