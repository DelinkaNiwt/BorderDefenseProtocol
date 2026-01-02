using JetBrains.Annotations;
using RimWorld;

namespace AlienRace;

[UsedImplicitly]
public class Thought_XenophobeVsXenophile : Thought_SituationalSocial
{
	public override float OpinionOffset()
	{
		if (pawn.story.traits.DegreeOfTrait(AlienDefOf.HAR_Xenophobia) != 1)
		{
			return -15f;
		}
		return -25f;
	}
}
