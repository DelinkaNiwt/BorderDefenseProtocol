using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(SkyfallerMaker), "SpawnSkyfaller", new Type[]
{
	typeof(ThingDef),
	typeof(ThingDef),
	typeof(IntVec3),
	typeof(Map)
})]
public static class Patch_SpawnSkyfaller_2
{
	public static bool Prefix(ThingDef skyfaller, ref IntVec3 pos, Map map)
	{
		return SkyfallerRedirectUtility.RedirectSkyfallerPos(ref pos, map, null, skyfaller);
	}
}
