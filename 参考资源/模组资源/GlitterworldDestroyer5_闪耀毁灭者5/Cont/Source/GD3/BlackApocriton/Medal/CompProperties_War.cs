using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GD3
{
	public class CompProperties_War : CompProperties
	{
		public CompProperties_War()
		{
			this.compClass = typeof(CompWar);
		}

		public string toggleLabelKey;

		public string toggleDescKey;

		public string toggleLabelKey2;

		public string toggleDescKey2;

		public string toggleIconPath;
	}
}
