using HarmonyLib;
using RimWorld.Planet;

namespace Milira;

[HarmonyPatch(typeof(Settlement))]
[HarmonyPatch("Visitable", MethodType.Getter)]
public static class MiliraPatch_Settlement_Visitable
{
	public static void Postfix(ref bool __result, Settlement __instance)
	{
		if (!__result && __instance.Faction.def == MiliraDefOf.Milira_Faction)
		{
			__result = true;
		}
	}
}
