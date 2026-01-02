using HarmonyLib;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(MechNameDisplayModeExtension), "ShouldDisplayMechName")]
internal class Ancot_PawnUIOverlay_DrawPawnGUIOverlay_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(Pawn mech, ref bool __result)
	{
		if (mech.IsColonyMech)
		{
			CompMechAutoFight compMechAutoFight = mech.TryGetComp<CompMechAutoFight>();
			if (compMechAutoFight != null && compMechAutoFight.AutoFight)
			{
				__result = true;
				return false;
			}
		}
		return true;
	}
}
