using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace AlienRace;

[UsedImplicitly]
public class ThoughtWorker_AlienVsXenophobia : ThoughtWorker
{
	protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
	{
		if (!Utilities.DifferentRace(p.def, otherPawn.def) || !RelationsUtility.PawnsKnowEachOther(p, otherPawn))
		{
			return false;
		}
		if (!otherPawn.story.traits.HasTrait(AlienDefOf.HAR_Xenophobia))
		{
			return false;
		}
		if (otherPawn.story.traits.DegreeOfTrait(AlienDefOf.HAR_Xenophobia) != -1)
		{
			return ThoughtState.ActiveAtStage(1);
		}
		return ThoughtState.ActiveAtStage(0);
	}
}
