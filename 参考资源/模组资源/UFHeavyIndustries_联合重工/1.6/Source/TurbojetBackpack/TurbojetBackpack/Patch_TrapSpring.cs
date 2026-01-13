using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Building_Trap), "Spring", new Type[] { typeof(Pawn) })]
public static class Patch_TrapSpring
{
	[HarmonyPriority(800)]
	public static bool Prefix(Building_Trap __instance, Pawn p)
	{
		if (p == null)
		{
			return true;
		}
		CompTurbojetFlight flightComp = TurbojetGlobal.GetFlightComp(p);
		if (flightComp == null)
		{
			return true;
		}
		if (flightComp.FlightMode != TurbojetMode.Off)
		{
			return false;
		}
		return true;
	}
}
