using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompShuttleAerocraft : CompShuttle
{
	private Building_Aerocraft Aerocraft => parent as Building_Aerocraft;

	public override bool IsAllowedNow(Thing t)
	{
		if (parent is Building_Aerocraft { FlightState: AerocraftState.Grounded })
		{
			return base.IsAllowedNow(t);
		}
		return false;
	}

	public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
	{
		if (Aerocraft.FlightState != AerocraftState.Grounded)
		{
			yield break;
		}
		foreach (FloatMenuOption item in base.CompFloatMenuOptions(selPawn))
		{
			yield return item;
		}
	}

	public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
	{
		if (Aerocraft.FlightState != AerocraftState.Grounded)
		{
			yield break;
		}
		foreach (FloatMenuOption item in base.CompMultiSelectFloatMenuOptions(selPawns))
		{
			yield return item;
		}
	}
}
