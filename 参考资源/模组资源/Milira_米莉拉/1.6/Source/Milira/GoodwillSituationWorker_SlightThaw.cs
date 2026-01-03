using RimWorld;

namespace Milira;

public class GoodwillSituationWorker_SlightThaw : GoodwillSituationWorker
{
	public override int GetMaxGoodwill(Faction other)
	{
		if (Faction.OfPlayerSilentFail == null)
		{
			return 100;
		}
		if (Active(other))
		{
			return -60;
		}
		return 100;
	}

	public static bool Active(Faction other)
	{
		if (other.def == MiliraDefOf.Milira_Faction && MiliraGameComponent_OverallControl.OverallControl.turnToFriend)
		{
			return true;
		}
		return false;
	}
}
