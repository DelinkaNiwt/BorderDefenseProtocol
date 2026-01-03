using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Pawn_FlightTracker))]
[HarmonyPatch("GetBestFlyAnimation")]
public static class MilianPatch_Pawn_FlightTracker_GetBestFlyAnimation
{
	[HarmonyPrefix]
	public static bool Prefix(Pawn pawn, Rot4? facingOverride, ref AnimationDef __result)
	{
		if (pawn.RaceProps.body.defName == "Milira_Body")
		{
			switch ((facingOverride ?? pawn.Rotation).AsInt)
			{
			case 0:
				__result = MiliraDefOf.Milira_FlyNorth;
				break;
			case 1:
				__result = MiliraDefOf.Milira_FlyEast;
				break;
			case 2:
				__result = MiliraDefOf.Milira_FlySouth;
				break;
			case 3:
				__result = MiliraDefOf.Milira_FlyWest;
				break;
			default:
				__result = MiliraDefOf.Milira_FlySouth;
				break;
			}
			return false;
		}
		return true;
	}
}
