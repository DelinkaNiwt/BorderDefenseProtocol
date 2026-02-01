using HarmonyLib;
using RimWorld;
using VanillaPsycastsExpanded.UI;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Pawn_PsychicEntropyTracker), "GetGizmo")]
public static class Pawn_EntropyTracker_GetGizmo_Prefix
{
	[HarmonyPrefix]
	public static void Prefix(Pawn_PsychicEntropyTracker __instance, ref Gizmo ___gizmo)
	{
		if (___gizmo == null)
		{
			___gizmo = new PsychicStatusGizmo(__instance);
		}
	}
}
