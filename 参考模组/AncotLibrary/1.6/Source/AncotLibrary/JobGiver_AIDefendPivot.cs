using RimWorld;
using Verse;

namespace AncotLibrary;

public class JobGiver_AIDefendPivot : JobGiver_AIDefendPawn
{
	protected override Pawn GetDefendee(Pawn pawn)
	{
		return pawn.TryGetComp<CompCommandTerminal>().pivot;
	}

	protected override float GetFlagRadius(Pawn pawn)
	{
		return 5f;
	}
}
