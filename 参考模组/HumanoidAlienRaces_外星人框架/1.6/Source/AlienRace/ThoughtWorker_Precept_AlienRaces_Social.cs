using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace AlienRace;

[UsedImplicitly]
public class ThoughtWorker_Precept_AlienRaces_Social : ThoughtWorker_Precept_Social
{
	protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
	{
		return Utilities.DifferentRace(p.def, otherPawn.def);
	}
}
