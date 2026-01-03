using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(Pawn_OutfitTracker), "get_CurrentApparelPolicy")]
public static class CurrentApperalPolicy_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(Pawn_OutfitTracker __instance, ref ApparelPolicy ___curApparelPolicy, ref ApparelPolicy __result)
	{
		if (!AncotLibrarySettings.apparelPolicy_AutoSetForColonist)
		{
			return true;
		}
		if (__instance.pawn.IsMutant && (__instance.pawn.mutant.Def.disableApparel || __instance.pawn.mutant.Def.disablePolicies))
		{
			return true;
		}
		if (___curApparelPolicy != null)
		{
			return true;
		}
		Dictionary<ThingDef, ApparelPolicy> raceApparelPolicy = GameComponent_AncotLibrary.GC.raceApparelPolicy;
		if (raceApparelPolicy.NullOrEmpty())
		{
			ApparelPolicyGenerator.GenerateApparelPolicyFromDef(out raceApparelPolicy);
			GameComponent_AncotLibrary.GC.raceApparelPolicy = raceApparelPolicy;
		}
		ThingDef def = __instance.pawn.def;
		if (___curApparelPolicy == null && raceApparelPolicy.ContainsKey(def))
		{
			ApparelPolicy apparelPolicy = raceApparelPolicy.TryGetValue(def);
			if (apparelPolicy == null && !ApparelPolicyGenerator.GenerateApparelPolicyFromDef(def, out apparelPolicy))
			{
				return true;
			}
			if (apparelPolicy != null)
			{
				___curApparelPolicy = apparelPolicy;
				__result = apparelPolicy;
				return false;
			}
		}
		return true;
	}
}
