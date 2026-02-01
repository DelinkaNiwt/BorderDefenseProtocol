using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GD3
{
	public class CompProperties_Famine : CompProperties
	{
		public CompProperties_Famine()
		{
			this.compClass = typeof(CompFamine);
		}

		public string toggleLabelKey;

		public string toggleDescKey;

		public string toggleLabelKey2;

		public string toggleDescKey2;

		public string toggleIconPath;
	}
}
