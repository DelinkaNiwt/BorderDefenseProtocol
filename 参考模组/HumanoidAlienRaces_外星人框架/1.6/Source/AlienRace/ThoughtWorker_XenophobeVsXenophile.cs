using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace AlienRace;

[UsedImplicitly]
public class ThoughtWorker_XenophobeVsXenophile : ThoughtWorker
{
	protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
	{
		return p.RaceProps.Humanlike && otherPawn.RaceProps.Humanlike && p.story.traits.HasTrait(AlienDefOf.HAR_Xenophobia) && otherPawn.story.traits.HasTrait(AlienDefOf.HAR_Xenophobia) && p.story.traits.DegreeOfTrait(AlienDefOf.HAR_Xenophobia) != otherPawn.story.traits.DegreeOfTrait(AlienDefOf.HAR_Xenophobia) && RelationsUtility.PawnsKnowEachOther(p, otherPawn);
	}
}
