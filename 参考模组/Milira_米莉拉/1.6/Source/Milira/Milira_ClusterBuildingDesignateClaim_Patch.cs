using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Designator_Claim))]
[HarmonyPatch("CanDesignateThing")]
public static class Milira_ClusterBuildingDesignateClaim_Patch
{
	[HarmonyPostfix]
	public static void Postfix(ref Thing t, ref AcceptanceReport __result)
	{
		Building building = t as Building;
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		if (building != null && building.Faction != null && building.Faction == faction && building.def.defName != "Milira_SunBlastFurnace")
		{
			__result = false;
		}
	}
}
