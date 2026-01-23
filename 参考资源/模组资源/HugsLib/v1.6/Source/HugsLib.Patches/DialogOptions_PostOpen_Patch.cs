using HarmonyLib;
using HugsLib.Settings;
using RimWorld;

namespace HugsLib.Patches;

[HarmonyPatch(typeof(Dialog_Options))]
[HarmonyPatch("PostOpen")]
internal class DialogOptions_PostOpen_Patch
{
	[HarmonyPostfix]
	public static void InjectHugsLibEntries(Dialog_Options __instance)
	{
		OptionsDialogExtensions.InjectHugsLibModEntries(__instance);
	}
}
