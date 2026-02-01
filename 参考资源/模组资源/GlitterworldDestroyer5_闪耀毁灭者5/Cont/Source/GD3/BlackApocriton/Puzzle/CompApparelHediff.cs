using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Text;
using UnityEngine;

namespace GD3
{
	public class CompApparelHediff : ThingComp
	{
		public CompProperties_ApparelHediff Props
		{
			get
			{
				return (CompProperties_ApparelHediff)this.props;
			}
		}

		public Apparel Coat
        {
            get
            {
				return this.parent as Apparel;
            }
        }

		public Pawn Owner
        {
            get
            {
				return this.Coat.Wearer;
            }
        }

		public override void CompTick()
		{
			base.CompTick();
			bool flag = this.parent != null && this.Owner != null;
			if (flag)
			{
				this.readyToUseTicks++;
				if (readyToUseTicks >= 61)
				{
					this.readyToUseTicks = 0;
					this.TrySetHediff(Owner, this.Props.hediff);
				}
			}
		}

		private void TrySetHediff(Pawn pawn, HediffDef hediffDef)
		{
			Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef, false);
			float level = this.TryGetLevel() + 0.1f;
			if (hediff == null)
			{
				hediff = pawn.health.AddHediff(hediffDef, pawn.health.hediffSet.GetBrain(), null, null);
				hediff.Severity = level;
				return;
			}
			HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
			if (hediffComp_Disappears != null)
			{
				hediffComp_Disappears.ticksToDisappear = 90;
			}
			if (hediff.Severity != level)
            {
				hediff.Severity = level;
            }
		}

		private int TryGetLevel()
        {
			if (Find.World.GetComponent<MainComponent>().list_str != null)
            {
				return Find.World.GetComponent<MainComponent>().list_str.Count;
            }
			Find.World.GetComponent<MainComponent>().list_str = new List<string>();
			return 0;
        }

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
		}

		private int readyToUseTicks = 0;

	}
}