using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[]
{
	typeof(Pawn),
	typeof(IntVec3)
})]
public static class Patch_CostToMoveIntoCell
{
	public static bool Prefix(Pawn pawn, IntVec3 c, ref float __result)
	{
		CompTurbojetFlight flightComp = TurbojetGlobal.GetFlightComp(pawn);
		if (pawn.Drafted && flightComp != null && flightComp.FlightMode != TurbojetMode.Off)
		{
			float num = ((flightComp.Extension != null) ? flightComp.Extension.flightMoveSpeed : 6f);
			if (num <= 0f)
			{
				num = 1f;
			}
			float num2 = 60f / num;
			if (c.x != pawn.Position.x && c.z != pawn.Position.z)
			{
				num2 *= 1.414f;
			}
			__result = num2;
			return false;
		}
		return true;
	}
}
