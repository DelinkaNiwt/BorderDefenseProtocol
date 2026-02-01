using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
	public class HediffCompProperties_RemoveWhenPlayer : HediffCompProperties
	{
		public HediffCompProperties_RemoveWhenPlayer()
		{
			this.compClass = typeof(HediffComp_RemoveWhenPlayer);
		}
	}

	public class HediffComp_RemoveWhenPlayer : HediffComp
	{
		public HediffCompProperties_RemoveWhenPlayer Props
		{
			get
			{
				return (HediffCompProperties_RemoveWhenPlayer)this.props;
			}
		}

		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			this.ticks++;
			if (this.ticks > 257)
            {
				this.ticks = 0;
				Pawn p = this.Pawn;
				if (p != null && p.Faction != null && p.Faction == Faction.OfPlayer)
                {
					p.health.RemoveHediff(this.parent);
                }
            }
		}

		private int ticks = 0;
	}
}
