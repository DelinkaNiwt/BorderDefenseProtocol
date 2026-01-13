using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(SkyfallerMaker), "SpawnSkyfaller", new Type[]
{
	typeof(ThingDef),
	typeof(IEnumerable<Thing>),
	typeof(IntVec3),
	typeof(Map)
})]
public static class Patch_SpawnSkyfaller_4
{
	public static bool Prefix(ThingDef skyfaller, IEnumerable<Thing> things, ref IntVec3 pos, Map map)
	{
		Faction incomingFaction = null;
		if (things != null)
		{
			foreach (Thing thing in things)
			{
				if (thing.Faction != null)
				{
					incomingFaction = thing.Faction;
					break;
				}
			}
		}
		return SkyfallerRedirectUtility.RedirectSkyfallerPos(ref pos, map, incomingFaction, skyfaller);
	}
}
