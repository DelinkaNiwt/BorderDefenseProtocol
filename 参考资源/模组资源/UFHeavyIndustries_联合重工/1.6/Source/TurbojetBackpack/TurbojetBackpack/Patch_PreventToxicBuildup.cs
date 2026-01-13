using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[]
{
	typeof(Hediff),
	typeof(BodyPartRecord),
	typeof(DamageInfo?),
	typeof(DamageWorker.DamageResult)
})]
public static class Patch_PreventToxicBuildup
{
	public static bool Prefix(Pawn_HealthTracker __instance, Hediff hediff, Pawn ___pawn)
	{
		if (hediff.def != HediffDefOf.ToxicBuildup)
		{
			return true;
		}
		if (___pawn == null)
		{
			return true;
		}
		CompTurbojetFlight flightComp = TurbojetGlobal.GetFlightComp(___pawn);
		if (flightComp != null && (flightComp.ShouldBeFlying || flightComp.CurrentHeight > 0.1f))
		{
			Map map = ___pawn.Map;
			if (map != null)
			{
				TerrainDef terrain = ___pawn.Position.GetTerrain(map);
				if (terrain != null && terrain.toxicBuildupFactor > 0f)
				{
					return false;
				}
			}
		}
		return true;
	}
}
