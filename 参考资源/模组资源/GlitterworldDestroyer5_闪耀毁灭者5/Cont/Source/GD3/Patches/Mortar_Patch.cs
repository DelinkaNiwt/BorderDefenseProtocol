using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace GD3
{
	[HarmonyPatch(typeof(Building_TurretGun), "get_CanSetForcedTarget")]
	public static class Mortar_Patch
	{
		public static bool Prefix(Building_TurretGun __instance, ref bool __result)
		{
			if ((__instance.def.defName == "Turret_GiantAutoMortar_Script" || __instance.def.defName == "PlayerTurret_AutoMortar" || __instance.def.defName == "PlayerTurret_LandingAutoMortar" || __instance.def.defName == "PlayerTurret_EMPArtillery") && __instance.Faction != null && __instance.Faction == Faction.OfPlayer)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}
}