using HarmonyLib;
using RimWorld;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(FireUtility), "TryAttachFire")]
public static class Patch_FireUtility_TryAttachFire
{
	public static bool Prefix(Thing t, float fireSize)
	{
		if (t is Pawn pawn)
		{
			CompTurbojetFlight flightComp = TurbojetGlobal.GetFlightComp(pawn);
			if (flightComp != null && (flightComp.ShouldBeFlying || flightComp.CurrentHeight > 0.1f))
			{
				return false;
			}
		}
		return true;
	}
}
