using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalLowEnergy_Drone : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		CompDrone compDrone = pawn.TryGetComp<CompDrone>();
		if (compDrone != null && compDrone.depleted)
		{
			return true;
		}
		return false;
	}
}
