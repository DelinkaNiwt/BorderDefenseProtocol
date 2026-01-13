using HarmonyLib;
using Verse;

namespace HarmonyMod;

[HarmonyPatch(typeof(Log), "ResetMessageCount")]
internal static class Log_ResetMessageCount_Patch
{
	public static void Postfix()
	{
		ExceptionTools.seenStacktraces.Clear();
	}
}
