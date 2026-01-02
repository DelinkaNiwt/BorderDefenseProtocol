using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompRefuelableAerocraft : CompRefuelable
{
	private Building_Aerocraft Aerocraft => parent as Building_Aerocraft;

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Aerocraft.FlightState == AerocraftState.Grounded)
		{
			foreach (Gizmo item in base.CompGetGizmosExtra())
			{
				yield return item;
			}
		}
		else if ((!base.Props.hideGizmosIfNotPlayerFaction || parent.Faction == Faction.OfPlayer) && Find.Selector.SelectedObjects.Count == 1)
		{
			yield return new Gizmo_SetFuelLevelAerocraftNotGrounded(this);
		}
	}

	public override void PostDraw()
	{
		if (Aerocraft.FlightState == AerocraftState.Grounded)
		{
			base.PostDraw();
		}
	}
}
