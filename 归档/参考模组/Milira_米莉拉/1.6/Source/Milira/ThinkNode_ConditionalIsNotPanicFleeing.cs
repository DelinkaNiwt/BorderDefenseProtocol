using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class ThinkNode_ConditionalIsNotPanicFleeing : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		return IsPawnPanicFleeing(pawn);
	}

	private bool IsPawnPanicFleeing(Pawn pawn)
	{
		if (pawn.MentalState != null && pawn.MentalState.def == MentalStateDefOf.PanicFlee)
		{
			return false;
		}
		return true;
	}
}
