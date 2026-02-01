using System;
using Verse;
using RimWorld;

namespace GD3
{
    public class CompProperties_CompAnalyzableUnlockScript : CompProperties_Analyzable
	{
		public int analysisID;

		public CompProperties_CompAnalyzableUnlockScript()
		{
			compClass = typeof(CompAnalyzableUnlockScript);
		}
	}
}
