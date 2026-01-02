using HarmonyLib;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(MechanitorUtility))]
[HarmonyPatch("CanDraftMech")]
public static class Ancot_MechanitorUtility_CanDraftMech_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Pawn mech, ref AcceptanceReport __result)
	{
		if (!__result.Accepted)
		{
			CompDraftable compDraftable = mech.TryGetComp<CompDraftable>();
			if (compDraftable != null)
			{
				__result = (compDraftable.Draftable ? ((AcceptanceReport)true) : ((AcceptanceReport)"IsLowEnergySelfShutdown".Translate(mech.Named("PAWN"))));
			}
		}
	}
}
