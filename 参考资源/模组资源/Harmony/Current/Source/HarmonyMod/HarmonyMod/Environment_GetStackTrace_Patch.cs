using System;
using System.Diagnostics;
using HarmonyLib;

namespace HarmonyMod;

[HarmonyPatch(typeof(Environment), "GetStackTrace")]
internal static class Environment_GetStackTrace_Patch
{
	public static bool Prefix(Exception e, bool needFileInfo, ref string __result)
	{
		if (Settings.noStacktraceEnhancing)
		{
			return true;
		}
		try
		{
			StackTrace trace = ((e == null) ? new StackTrace(needFileInfo) : new StackTrace(e, needFileInfo));
			__result = ExceptionTools.ExtractHarmonyEnhancedStackTrace(trace, forceRefresh: false, out var _);
			return false;
		}
		catch (Exception)
		{
			return true;
		}
	}
}
