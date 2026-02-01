using System;
using Verse;
using RimWorld;

namespace GD3
{
	public class CompWallSelfHeal : ThingComp
	{
		public CompProperties_WallSelfHeal Props
		{
			get
			{
				return (CompProperties_WallSelfHeal)this.props;
			}
		}

		public override void PostExposeData()
		{
			Scribe_Values.Look<int>(ref this.ticksPassedSinceLastHeal, "ticksPassedSinceLastHeal", 0, false);
		}

		public override void CompTick()
		{
			this.Tick(1);
		}

		public override void CompTickRare()
		{
			this.Tick(250);
		}

		public override void CompTickLong()
		{
			this.Tick(2000);
		}

		private void Tick(int ticks)
		{
			this.ticksPassedSinceLastHeal += ticks;
			if (this.ticksPassedSinceLastHeal >= this.Props.ticksPerHeal)
			{
				this.ticksPassedSinceLastHeal = 0;
				if (this.parent.HitPoints < this.parent.MaxHitPoints)
				{
					ThingWithComps parent = this.parent;
					int hitPoints = parent.HitPoints;
					if (hitPoints + 500 >= this.parent.MaxHitPoints)
                    {
						parent.HitPoints = this.parent.MaxHitPoints;
						return;
					}
					parent.HitPoints = hitPoints + 500;
					Effecter effecter = EffecterDefOf.ConstructMetal.SpawnAttached(parent, parent.MapHeld, 1f);
					effecter.Trigger(parent, parent, 25);
					effecter.Cleanup();
				}
			}
		}

		public int ticksPassedSinceLastHeal;
	}
}