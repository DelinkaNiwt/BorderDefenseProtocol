using HarmonyLib;
using RimWorld;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(Pawn_DraftController))]
[HarmonyPatch("ShowDraftGizmo", MethodType.Getter)]
public static class Ancot_DraftController_ShowDraftGizmo_Patch
{
	public static void Postfix(ref bool __result, Pawn_DraftController __instance)
	{
		if (!__result)
		{
			CompDraftable compDraftable = __instance.pawn.TryGetComp<CompDraftable>();
			if (compDraftable != null && compDraftable.Draftable)
			{
				__result = true;
			}
		}
	}
}
