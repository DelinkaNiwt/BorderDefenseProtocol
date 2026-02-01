using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GD3
{
	public class CompProperties_Plague : CompProperties
	{
		public CompProperties_Plague()
		{
			this.compClass = typeof(CompPlague);
		}

		public string toggleLabelKey;

		public string toggleDescKey;

		public string toggleLabelKey2;

		public string toggleDescKey2;

		public string toggleIconPath;
	}
}
