using Verse;
using Verse.AI;

namespace AncotLibrary;

public abstract class JobGiver_GetDroneEnergy : ThinkNode_JobGiver
{
	public bool forced;

	public static float GetMinAutorechargeThreshold(Pawn pawn)
	{
		return pawn.TryGetComp<CompDrone>().PercentRecharge;
	}

	protected virtual bool ShouldAutoRecharge(Pawn pawn)
	{
		CompDrone compDrone = pawn.TryGetComp<CompDrone>();
		if (compDrone == null)
		{
			return false;
		}
		float percentFull = compDrone.PercentFull;
		float num = GetMinAutorechargeThreshold(pawn);
		return (forced || percentFull < num || percentFull < 0.1f) && percentFull < 0.95f;
	}

	public override float GetPriority(Pawn pawn)
	{
		if (!ShouldAutoRecharge(pawn))
		{
			return 0f;
		}
		return 9.5f;
	}
}
