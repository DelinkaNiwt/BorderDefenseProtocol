using HarmonyLib;
using UnityEngine;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Pawn), "DrawPos", MethodType.Getter)]
public static class Patch_Pawn_DrawPos
{
	public static void Postfix(Pawn __instance, ref Vector3 __result)
	{
		if (!__instance.Spawned || __instance.Downed || __instance.Map == null)
		{
			return;
		}
		CompTurbojetFlight flightComp = TurbojetGlobal.GetFlightComp(__instance);
		if (flightComp != null && flightComp.CurrentHeight > 0f)
		{
			float num = flightComp.CurrentHeight + flightComp.GetBreathingOffset();
			float num2 = (float)__instance.Map.Size.z - 1.5f;
			if (__result.z + num > num2)
			{
				float b = Mathf.Max(0f, num2 - __result.z);
				num = Mathf.Min(num, b);
			}
			__result.z += num;
			__result.y += 0.02f;
		}
	}
}
