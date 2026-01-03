using RimWorld;
using Verse;

namespace Milira;

public class ThoughtWorker_MiliraHateWearingBloodStain : ThoughtWorker
{
	protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
	{
		if (!p.RaceProps.Humanlike || !other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(p, other) || p.def.defName == "Milira_Race")
		{
			return false;
		}
		foreach (Apparel item in other.apparel.WornApparel)
		{
			if (item.def.MadeFromStuff && item.Stuff != null && item.Stuff.defName.Equals("Milira_BloodStainedFeather"))
			{
				return true;
			}
		}
		return false;
	}
}
