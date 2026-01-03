using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompTransporterAerocraft : CompTransporter
{
	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		ThingWithComps thingWithComps = parent;
		if (!(thingWithComps is Building_Aerocraft { FlightState: AerocraftState.Grounded }))
		{
			yield break;
		}
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
	}
}
