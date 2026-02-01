using System;
using Verse;

namespace GD3
{
	public class CompProperties_SecVerb : CompProperties
	{
		public CompProperties_SecVerb()
		{
			this.compClass = typeof(CompSecVerb);
		}

		public VerbProperties verbProps = new VerbProperties();

		public float range;
	}
}
