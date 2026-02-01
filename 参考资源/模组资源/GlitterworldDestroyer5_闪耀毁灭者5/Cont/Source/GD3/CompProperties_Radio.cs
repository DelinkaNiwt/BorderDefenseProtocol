using System;
using Verse;

namespace GD3
{
	public class CompProperties_Radio : CompProperties
	{
		public CompProperties_Radio()
		{
			this.compClass = typeof(CompRadio);
		}

		public float powerCostFst;

		public float powerCostSec;

		public float powerCostThd;
	}
}
