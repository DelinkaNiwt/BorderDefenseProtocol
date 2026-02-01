using System;
using RimWorld;
using Verse;

namespace GD3
{
	public class CompProperties_ApparelHediff : CompProperties
	{
		public CompProperties_ApparelHediff()
		{
			this.compClass = typeof(CompApparelHediff);
		}

		public HediffDef hediff;

	}
}
