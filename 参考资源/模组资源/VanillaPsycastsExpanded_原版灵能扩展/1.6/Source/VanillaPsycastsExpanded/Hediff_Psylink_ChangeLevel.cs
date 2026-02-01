using System;
using HarmonyLib;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Hediff_Psylink), "ChangeLevel", new Type[]
{
	typeof(int),
	typeof(bool)
})]
public static class Hediff_Psylink_ChangeLevel
{
	public static bool Prefix(Hediff_Psylink __instance, int levelOffset, ref bool sendLetter)
	{
		__instance.pawn.Psycasts().ChangeLevel(levelOffset, sendLetter);
		sendLetter = false;
		return false;
	}
}
