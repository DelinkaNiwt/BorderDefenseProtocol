using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(SkyfallerMaker), "SpawnSkyfaller", new Type[]
{
	typeof(ThingDef),
	typeof(Thing),
	typeof(IntVec3),
	typeof(Map)
})]
public static class Patch_SpawnSkyfaller_3
{
	public static bool Prefix(ThingDef skyfaller, Thing innerThing, ref IntVec3 pos, Map map)
	{
		return SkyfallerRedirectUtility.RedirectSkyfallerPos(ref pos, map, innerThing?.Faction, skyfaller);
	}
}
