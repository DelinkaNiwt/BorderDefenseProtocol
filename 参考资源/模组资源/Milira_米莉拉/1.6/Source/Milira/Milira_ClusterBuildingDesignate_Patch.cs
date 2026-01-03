using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Designator_Deconstruct))]
[HarmonyPatch("CanDesignateThing")]
public static class Milira_ClusterBuildingDesignate_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Thing t, ref AcceptanceReport __result)
	{
		if (t.GetInnerIfMinified() is Building building && building.def.category == ThingCategory.Building)
		{
			Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
			if (building.Faction != null && building.Faction == faction && building.def.building.IsDeconstructible)
			{
				__result = new AcceptanceReport("MilianCluster".Translate());
			}
		}
	}
}
