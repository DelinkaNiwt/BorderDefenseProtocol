using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalRecharging_Drone : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		return pawn.TryGetComp<CompDrone>()?.currentCharger != null;
	}
}
