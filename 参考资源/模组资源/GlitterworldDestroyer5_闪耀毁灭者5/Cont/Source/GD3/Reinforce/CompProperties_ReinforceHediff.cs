using System;
using Verse;
using System.Collections.Generic;

namespace GD3
{
	public class CompProperties_ReinforceHediff : CompProperties
	{
		public CompProperties_ReinforceHediff()
		{
			this.compClass = typeof(CompReinforceHediff);
		}

		public List<HediffDef> hediffsRange;

		public List<HediffDef> hediffsMelee;
	}
}
