using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib.Settings;
using RimWorld;
using Verse;

namespace HugsLib.Patches;

[HarmonyPatch(typeof(Dialog_Options))]
[HarmonyPatch("DoModOptions")]
[HarmonyPatch(new Type[] { typeof(Listing_Standard) })]
internal class DialogOptions_DoModOptions_Patch
{
	private static bool patched;

	[HarmonyCleanup]
	public static void Cleanup()
	{
		if (!patched)
		{
			HugsLibController.Logger.Error("DialogOptions_DoModOptions_Patch could not be applied.");
		}
	}

	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> InterceptWindowOpening(IEnumerable<CodeInstruction> instructions)
	{
		patched = false;
		ConstructorInfo modSettingsWindowConstructorInfo = typeof(RimWorld.Dialog_ModSettings).GetConstructor(new Type[1] { typeof(Mod) });
		if (modSettingsWindowConstructorInfo == null)
		{
			throw new Exception("Failed to reflect required method");
		}
		foreach (CodeInstruction inst in instructions)
		{
			if (!patched && inst.Is(OpCodes.Newobj, modSettingsWindowConstructorInfo))
			{
				yield return new CodeInstruction(OpCodes.Call, new Func<Mod, Window>(OptionsDialogExtensions.GetModSettingsWindow).Method);
				patched = true;
			}
			else
			{
				yield return inst;
			}
		}
	}
}
