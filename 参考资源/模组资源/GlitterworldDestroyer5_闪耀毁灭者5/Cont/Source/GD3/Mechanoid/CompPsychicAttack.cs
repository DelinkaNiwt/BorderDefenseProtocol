using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
	public class CompProperties_PsychicAttack : CompProperties
	{
		public CompProperties_PsychicAttack()
		{
			this.compClass = typeof(CompPsychicAttack);
		}

		public float minDistance;

		public IntRange interval;

		public int actPeriod;

		public HediffDef hediffToAdd;

		public HediffDef hediffDef;
	}

	public class CompPsychicAttack : ThingComp
	{
		public CompProperties_PsychicAttack Props
		{
			get
			{
				return (CompProperties_PsychicAttack)this.props;
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			ThingWithComps parent = this.parent;
			Pawn pawn = parent as Pawn;
			bool flag = pawn != null && pawn.Spawned && Find.TickManager.TicksGame >= this.readyToUseTicks && !pawn.Downed;
			if (flag)
			{
				this.readyToUseTicks = Find.TickManager.TicksGame + this.Props.interval.RandomInRange;
				IEnumerable<Pawn> enumerable = from x in pawn.Map.mapPawns.AllPawns
											   where x.Position.DistanceTo(pawn.Position) < this.Props.minDistance
											   select x;
				//FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 1.0f);
				foreach (Pawn pawn2 in enumerable)
				{
					Hediff plagueOnPawn = pawn2.health?.hediffSet?.GetFirstHediffOfDef(Props.hediffToAdd);
					bool flag2 = pawn2 != pawn && pawn2.RaceProps.IsFlesh && pawn2.Faction != pawn.Faction;
					if (flag2)
					{
						int num = this.Props.actPeriod;
						if (plagueOnPawn != null)
						{
							Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffToAdd, pawn2);
							HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
							if (hediffComp_Disappears != null)
							{
								hediffComp_Disappears.ticksToDisappear = 60;
							}
							pawn2.health.RemoveHediff(hediff);
							pawn2.health.AddHediff(hediff);
						}
						else
						{
							Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffToAdd, pawn2);
							HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
							if (hediffComp_Disappears != null)
							{
								hediffComp_Disappears.ticksToDisappear = 60;
							}
							pawn2.health.AddHediff(hediff);
						}
					}
				}
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
		}

		private int readyToUseTicks = 0;
	}
}
