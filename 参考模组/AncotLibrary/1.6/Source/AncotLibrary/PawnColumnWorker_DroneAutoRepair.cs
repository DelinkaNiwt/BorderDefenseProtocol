using RimWorld;
using Verse;

namespace AncotLibrary;

public class PawnColumnWorker_DroneAutoRepair : PawnColumnWorker_Checkbox
{
	protected override bool HasCheckbox(Pawn pawn)
	{
		CompDrone compDrone = pawn.TryGetComp<CompDrone>();
		if (compDrone != null)
		{
			return true;
		}
		return false;
	}

	protected override bool GetValue(Pawn pawn)
	{
		CompDrone compDrone = pawn.TryGetComp<CompDrone>();
		return compDrone.autoRepair;
	}

	protected override void SetValue(Pawn pawn, bool value, PawnTable table)
	{
		CompDrone compDrone = pawn.TryGetComp<CompDrone>();
		compDrone.autoRepair = value;
	}
}
