using System.Reflection;
using HarmonyLib;
using Verse;

namespace NCL_Storyteller;

public static class NCL_StorytellerUtility
{
	public static NCL_Storyteller_Settings NCL_Storyteller_Settings = LoadedModManager.GetMod<NCL_Storyteller_Mod>().GetSettings<NCL_Storyteller_Settings>();

	public static MethodInfo MaxDefaultThreatPointsNow_Method = AccessTools.Method(typeof(NCL_StorytellerUtility), "MaxDefaultThreatPointsNow");

	public static MethodInfo GetAdditionWealthCurveValue_Method = AccessTools.Method(typeof(NCL_StorytellerUtility), "GetAdditionWealthCurveValue");

	public static bool IsNCLStoryteller()
	{
		return Find.Storyteller.def.defName == "NCL_Justice_Storyteller";
	}

	public static float MaxDefaultThreatPointsNow()
	{
		if (!IsNCLStoryteller())
		{
			return 10000f;
		}
		return NCL_Storyteller_Settings.maxDefaultThreatPoints;
	}

	public static int MaxPawn()
	{
		if (!IsNCLStoryteller())
		{
			return int.MaxValue;
		}
		return NCL_Storyteller_Settings.maxPawns;
	}

	public static float GetAdditionWealthCurveValue(float playerWealthForStoryteller)
	{
		if (!IsNCLStoryteller() || playerWealthForStoryteller < 1000000f)
		{
			return 0f;
		}
		return (playerWealthForStoryteller - 1000000f) * 0.002f;
	}
}
