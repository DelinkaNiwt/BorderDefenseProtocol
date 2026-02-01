using System;
using Verse;

namespace GD3
{
	public class CompProperties_SecVerb_Pawn : CompProperties
	{
		public CompProperties_SecVerb_Pawn()
		{
			this.compClass = typeof(CompSecVerbPawn);
		}

		public VerbProperties verbProps = new VerbProperties();

		public string toggleLabelKey;

		public string toggleDescKey;

		public string toggleIconPath;

		public string fireModeA_Desc;

		public string fireModeB_Desc;
	}
}
