using System;
using Verse;
using RimWorld;

namespace GD3
{
	public class CompProperties_CompAnalyzableSubcore : CompProperties_Analyzable
	{
		public int analysisID;

		public CompProperties_CompAnalyzableSubcore()
		{
			compClass = typeof(CompAnalyzableSubcore);
		}
	}
}
