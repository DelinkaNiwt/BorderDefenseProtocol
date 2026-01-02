using JetBrains.Annotations;
using RimWorld;

namespace AlienRace;

[UsedImplicitly]
public class Thought_XenophobiaVsAlien : Thought_SituationalSocial
{
	public override float OpinionOffset()
	{
		return Utilities.DifferentRace(pawn.def, OtherPawn().def) ? ((pawn.story.traits.DegreeOfTrait(AlienDefOf.HAR_Xenophobia) == 1) ? (-30) : ((OtherPawn().story.traits.DegreeOfTrait(AlienDefOf.HAR_Xenophobia) == 1) ? (-15) : 0)) : 0;
	}
}
