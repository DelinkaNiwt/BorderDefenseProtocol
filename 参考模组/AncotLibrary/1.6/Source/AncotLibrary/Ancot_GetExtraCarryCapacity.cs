using System;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(new Type[]
{
	typeof(Pawn),
	typeof(StringBuilder)
})]
[HarmonyPatch(typeof(MassUtility))]
[HarmonyPatch("Capacity")]
public static class Ancot_GetExtraCarryCapacity
{
	[HarmonyPostfix]
	[HarmonyPriority(100)]
	public static void Postfix(Pawn p, ref float __result, StringBuilder explanation = null)
	{
		float statValue = p.GetStatValue(AncotDefOf.Ancot_ExtraCaravanCarryCapacity);
		if (explanation != null && statValue != 0f)
		{
			explanation.Append(" (+" + "Ancot.Extra".Translate() + statValue.ToString("F1") + " kg)");
		}
		if (GameComponent_AncotLibrary.GC.VanillaExpandedFramework_Active)
		{
			__result += statValue / 2f;
		}
		else
		{
			__result += statValue;
		}
	}
}
