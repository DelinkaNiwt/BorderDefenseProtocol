using System;
using Verse;

namespace GD3
{
	public class CompProperties_BerserkMarrow : CompProperties
	{
		public CompProperties_BerserkMarrow()
		{
			this.compClass = typeof(CompBerserkMarrow);
		}

		public int ticksToAffect;
	}
}
