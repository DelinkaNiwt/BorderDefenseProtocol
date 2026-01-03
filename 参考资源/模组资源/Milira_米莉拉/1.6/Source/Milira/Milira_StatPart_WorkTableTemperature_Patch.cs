using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(new Type[] { typeof(Thing) })]
[HarmonyPatch(typeof(StatPart_WorkTableTemperature))]
[HarmonyPatch("Applies")]
public static class Milira_StatPart_WorkTableTemperature_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Thing t, ref bool __result)
	{
		if (t != null && t.def.defName == "Milira_SunBlastFurnace")
		{
			__result = false;
		}
	}
}
