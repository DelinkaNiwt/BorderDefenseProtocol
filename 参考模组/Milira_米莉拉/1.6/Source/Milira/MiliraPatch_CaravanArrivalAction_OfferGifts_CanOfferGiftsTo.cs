using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace Milira;

[HarmonyPatch(typeof(CaravanArrivalAction_OfferGifts))]
[HarmonyPatch("CanOfferGiftsTo")]
public static class MiliraPatch_CaravanArrivalAction_OfferGifts_CanOfferGiftsTo
{
	public static void Postfix(ref FloatMenuAcceptanceReport __result, Caravan caravan, Settlement settlement, CaravanArrivalAction_OfferGifts __instance)
	{
		if (settlement.Faction.def == MiliraDefOf.Milira_Faction)
		{
			__result = FloatMenuAcceptanceReport.WasRejected;
		}
	}
}
