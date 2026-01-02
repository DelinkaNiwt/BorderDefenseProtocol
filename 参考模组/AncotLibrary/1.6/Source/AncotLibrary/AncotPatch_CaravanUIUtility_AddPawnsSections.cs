using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(CaravanUIUtility))]
[HarmonyPatch("AddPawnsSections")]
public static class AncotPatch_CaravanUIUtility_AddPawnsSections
{
	[HarmonyPostfix]
	public static void Postfix(ref TransferableOneWayWidget widget, List<TransferableOneWay> transferables)
	{
		if (ModsConfig.BiotechActive)
		{
			IEnumerable<TransferableOneWay> source = transferables.Where((TransferableOneWay x) => x.ThingDef.category == ThingCategory.Pawn);
			widget.AddSection("Ancot.DroneSection".Translate(), source.Where((TransferableOneWay x) => ((Pawn)x.AnyThing).IsColonyMech && ((Pawn)x.AnyThing).TryGetComp<CompDrone>() != null));
		}
	}
}
