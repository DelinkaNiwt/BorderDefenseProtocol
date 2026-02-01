using System;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace GD3
{
	/*[HarmonyPatch(typeof(SitePart), "ExposeData")]
	public static class SitePart_Patch
	{
		public static void Postfix(SitePart __instance)
		{
			if (__instance.def.Worker is SitePartWorker_Militor worker)
            {
				if (GDSettings.DeveloperMode)
                {
					Log.Warning("Exposed: Militor.");
                }
				Scribe_References.Look(ref worker.pawn, "m_pawn");
				Scribe_Deep.Look(ref worker.thing, "m_thing");
			}
			else if (__instance.def.Worker is SitePartWorker_Map worker2 && worker2?.thing != null)
            {
				Scribe_Deep.Look(ref worker2.thing, "m_thing");
			}
		}
	}*/

	[HarmonyPatch(typeof(TimedDetectionRaids), "get_RaidFaction")]
	public static class TimedDetectionRaids_Patch
	{
		public static void Postfix(TimedDetectionRaids __instance, ref Faction __result)
		{
			if (__instance.parent.Faction == GDUtility.BlackMechanoid)
			{
				__result = Faction.OfMechanoids;
			}
		}
	}

	[HarmonyPatch(typeof(Site), "get_PreferredMapSize")]
	public static class SiteSize_Patch
	{
		public static void Postfix(Site __instance, ref IntVec3 __result)
		{
			if (__instance.parts.Any(p => p.def == GDDefOf.GD_Sitepart_BlackApocriton))
			{
				__result = new IntVec3(75, 1, 75);
			}
		}
	}
}