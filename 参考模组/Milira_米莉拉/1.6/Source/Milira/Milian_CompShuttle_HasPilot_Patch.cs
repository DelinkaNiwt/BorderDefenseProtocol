using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(CompShuttle))]
[HarmonyPatch("HasPilot", MethodType.Getter)]
public static class Milian_CompShuttle_HasPilot_Patch
{
	[HarmonyPostfix]
	public static void Postfix(ref bool __result, CompShuttle __instance)
	{
		if (__result)
		{
			return;
		}
		ThingOwner innerContainer = __instance.Transporter.innerContainer;
		for (int i = 0; i < innerContainer.Count; i++)
		{
			Pawn pawn = innerContainer[i] as Pawn;
			if (MilianUtility.IsMilian(pawn))
			{
				__result = true;
				break;
			}
		}
	}
}
