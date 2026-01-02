using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnColumnWorker_DroneAllowedArea : PawnColumnWorker_AllowedArea
{
	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		if (pawn.Faction == Faction.OfPlayer && pawn.RaceProps.IsMechanoid && pawn.TryGetComp<CompDrone>() != null)
		{
			if (pawn.playerSettings.SupportsAllowedAreas)
			{
				AreaAllowedGUI.DoAllowedAreaSelectors(rect, pawn);
			}
			else if (AnimalPenUtility.NeedsToBeManagedByRope(pawn))
			{
				AnimalPenGUI.DoAllowedAreaMessage(rect, pawn);
			}
		}
	}
}
