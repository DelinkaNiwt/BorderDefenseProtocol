using RimWorld;

namespace Milira;

public class Thought_MiliraHateWearingBloodStain : Thought_SituationalSocial
{
	public override float OpinionOffset()
	{
		if (ThoughtUtility.ThoughtNullified(pawn, def))
		{
			return 0f;
		}
		if (pawn.def.defName == "Milira_Race")
		{
			return -60f;
		}
		return 0f;
	}
}
