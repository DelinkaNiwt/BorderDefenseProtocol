using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_Hallucination : HediffWithComps
{
	public static List<ThoughtDef> thoughtsToChange = new List<ThoughtDef>
	{
		ThoughtDefOf.AteInImpressiveDiningRoom,
		ThoughtDefOf.JoyActivityInImpressiveRecRoom,
		ThoughtDefOf.SleptInBedroom,
		ThoughtDefOf.SleptInBarracks
	};

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		foreach (ThoughtDef item in thoughtsToChange)
		{
			pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(item)?.SetForcedStage(item.stages.Count - 1);
		}
	}
}
