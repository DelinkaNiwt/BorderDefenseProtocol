using System;
using RimWorld;
using Verse;

namespace GD3
{
	public class CompProperties_Terror : CompProperties
	{
		public CompProperties_Terror()
		{
			this.compClass = typeof(CompTerror);
		}

		public int range;

		public int interval = 60;

		public bool applyThought = true;

		public ThoughtDef thought;

		public HediffDef hediff;

		public float severityToAdd;

		public int terrorLevel;
	}
}
