using HarmonyLib;
using RimWorld;
using Verse;

namespace MechanitorCommandRange;

[HarmonyPatch(typeof(MechanitorUtility), "InMechanitorCommandRange")]
internal static class PatchMechanitorUtility_InMechanitorCommandRange
{
	private static bool Prefix(Pawn mech, LocalTargetInfo target, ref bool __result)
	{
		if (mech.RaceProps.IsMechanoid && mech.Faction == Faction.OfPlayer && mech.health.hediffSet.HasHediff(HediffDef.Named("NCL_King_of_the_field")))
		{
			__result = true;
			return false;
		}
		return true;
	}
}
