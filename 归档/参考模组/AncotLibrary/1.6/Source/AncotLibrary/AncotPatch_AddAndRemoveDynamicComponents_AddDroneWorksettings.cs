using HarmonyLib;
using RimWorld;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(PawnComponentsUtility))]
[HarmonyPatch("AddAndRemoveDynamicComponents")]
public static class AncotPatch_AddAndRemoveDynamicComponents_AddDroneWorksettings
{
	[HarmonyPostfix]
	public static void Postfix(Pawn pawn)
	{
		bool flag = pawn.Faction != null && pawn.Faction.IsPlayer;
		CompDrone compDrone = pawn.TryGetComp<CompDrone>();
		if (flag && compDrone != null && pawn.workSettings == null)
		{
			pawn.workSettings = new Pawn_WorkSettings(pawn);
		}
	}
}
