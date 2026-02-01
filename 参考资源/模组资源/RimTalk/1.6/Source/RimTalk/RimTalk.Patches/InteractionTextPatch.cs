using HarmonyLib;
using RimTalk.Data;
using Verse;

namespace RimTalk.Patches;

[HarmonyPatch]
public static class InteractionTextPatch
{
	public static bool IsRimTalkInteraction(LogEntry entry)
	{
		if (entry == null)
		{
			return false;
		}
		return (Find.World?.GetComponent<RimTalkWorldComponent>())?.RimTalkInteractionTexts.ContainsKey(entry.GetUniqueLoadID()) ?? false;
	}

	[HarmonyPatch(typeof(LogEntry), "ToGameStringFromPOV")]
	[HarmonyPostfix]
	public static void ToGameStringFromPOV_Postfix(LogEntry __instance, ref string __result)
	{
		RimTalkWorldComponent worldComp = Find.World?.GetComponent<RimTalkWorldComponent>();
		if (worldComp != null && worldComp.TryGetTextFor(__instance, out var customText))
		{
			__result = customText;
		}
	}
}
