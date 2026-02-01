using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(MeditationFocusTypeAvailabilityCache), "PawnCanUseInt")]
public static class MeditationFocusTypeAvailabilityCache_PawnCanUse_Patch
{
	public static void Postfix(Pawn p, MeditationFocusDef type, ref bool __result)
	{
		Hediff_PsycastAbilities hediff_PsycastAbilities = p.Psycasts();
		if (hediff_PsycastAbilities != null && hediff_PsycastAbilities.unlockedMeditationFoci.Contains(type))
		{
			__result = true;
			return;
		}
		MeditationFocusExtension modExtension = type.GetModExtension<MeditationFocusExtension>();
		if (modExtension != null && modExtension.pointsOnly)
		{
			__result = false;
		}
	}
}
