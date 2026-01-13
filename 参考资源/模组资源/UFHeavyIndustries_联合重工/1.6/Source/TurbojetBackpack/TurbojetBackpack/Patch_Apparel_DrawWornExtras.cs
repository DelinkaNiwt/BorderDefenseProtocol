using HarmonyLib;
using RimWorld;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Apparel), "DrawWornExtras")]
public static class Patch_Apparel_DrawWornExtras
{
	public static void Postfix(Apparel __instance)
	{
		__instance.GetComp<CompTurbojetShield>()?.DrawWornExtras();
	}
}
