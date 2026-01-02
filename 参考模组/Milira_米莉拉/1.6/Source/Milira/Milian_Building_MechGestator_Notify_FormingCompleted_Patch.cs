using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Building_MechGestator))]
[HarmonyPatch("Notify_FormingCompleted")]
public static class Milian_Building_MechGestator_Notify_FormingCompleted_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Building_MechGestator __instance)
	{
		Pawn pawn = __instance.innerContainer.FirstOrDefault((Thing t) => t is Pawn pawn2 && MilianUtility.IsMilian(pawn2)) as Pawn;
		if (pawn != null && pawn.Faction.IsPlayer && pawn.ageTracker.AgeChronologicalTicks == 0)
		{
			pawn.apparel.WornApparel.RemoveAll((Apparel _) => true);
			ThingWithComps eq = pawn.equipment?.Primary;
			pawn.equipment.Remove(eq);
			Apparel newApparel = (Apparel)ThingMaker.MakeThing(MiliraDefOf.Milian_GestateRobe);
			pawn.apparel.Wear(newApparel, dropReplacedApparel: false);
			Apparel newApparel2 = (Apparel)ThingMaker.MakeThing(MiliraDefOf.Milian_Lining, MiliraDefOf.Milira_FeatherThread);
			pawn.apparel.Wear(newApparel2, dropReplacedApparel: false);
		}
		if (pawn != null && pawn.workSettings != null && MiliraDefOf.Milira_MilianTech_WorkManagement.IsFinished && MiliraRaceSettings.MiliraRace_ModSetting_MilianDisableWorksGestate)
		{
			pawn.workSettings.DisableAll();
		}
	}
}
