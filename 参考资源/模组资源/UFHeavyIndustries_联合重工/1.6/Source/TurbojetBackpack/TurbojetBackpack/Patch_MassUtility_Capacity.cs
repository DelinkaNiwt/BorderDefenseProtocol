using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(MassUtility), "Capacity")]
public static class Patch_MassUtility_Capacity
{
	private static StatDef cachedFlightStat;

	public static void Postfix(Pawn p, StringBuilder explanation, ref float __result)
	{
		if (p == null || p.apparel == null)
		{
			return;
		}
		if (cachedFlightStat == null)
		{
			cachedFlightStat = DefDatabase<StatDef>.GetNamedSilentFail("AdditionalQualityCarry");
			if (cachedFlightStat == null)
			{
				return;
			}
		}
		float statValue = p.GetStatValue(cachedFlightStat);
		if (statValue > 0f)
		{
			__result += statValue;
			if (explanation != null)
			{
				explanation.AppendLine();
				explanation.Append("  - " + cachedFlightStat.LabelCap + ": +" + statValue.ToString("F0") + " kg");
			}
		}
	}
}
