using HarmonyLib;
using RimWorld;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
public static class Patch_Pawn_PreApplyDamage
{
	public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
	{
		if (dinfo.Def != DamageDefOf.Burn)
		{
			return true;
		}
		CompTurbojetFlight flightComp = TurbojetGlobal.GetFlightComp(__instance);
		if (flightComp == null)
		{
			return true;
		}
		if (!flightComp.ShouldBeFlying && !(flightComp.CurrentHeight > 0.1f))
		{
			return true;
		}
		if (dinfo.Instigator == null)
		{
			Map map = __instance.Map;
			if (map != null)
			{
				TerrainDef terrain = __instance.Position.GetTerrain(map);
				if (terrain != null && terrain.burnDamage > 0)
				{
					absorbed = true;
					return false;
				}
			}
		}
		return true;
	}
}
