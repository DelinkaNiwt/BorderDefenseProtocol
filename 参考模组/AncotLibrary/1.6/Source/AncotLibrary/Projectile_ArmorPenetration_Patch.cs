using HarmonyLib;
using RimWorld;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(Projectile))]
[HarmonyPatch("ArmorPenetration", MethodType.Getter)]
public static class Projectile_ArmorPenetration_Patch
{
	public static void Postfix(ref float __result, Projectile __instance)
	{
		Pawn pawn = __instance.Launcher as Pawn;
		float num = 1f;
		if (pawn != null)
		{
			num = pawn.GetStatValue(AncotDefOf.Ancot_ProjectileArmorPenetrationMultiplier);
		}
		__result *= num;
	}
}
