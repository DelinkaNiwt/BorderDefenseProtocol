using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib.Quickstart;
using Verse;

namespace HugsLib.Patches;

/// <summary>
/// Adds an entry point to draw and additional debug button on the toolbar.
/// The infix is necessary to catch the WidgetRow that the stock buttons are drawn to.
/// </summary>
[HarmonyPatch(typeof(DebugWindowsOpener))]
[HarmonyPatch("DrawButtons")]
internal class DebugWindowsOpener_Patch
{
	private static bool patched;

	[HarmonyPrepare]
	public static bool Prepare()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			if (!patched)
			{
				HugsLibController.Logger.Error("DebugWindowsOpener_Patch could not be applied.");
			}
		});
		return true;
	}

	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> DrawAdditionalButtons(IEnumerable<CodeInstruction> instructions)
	{
		patched = false;
		CodeInstruction[] instructionsArr = instructions.ToArray();
		FieldInfo widgetRowField = AccessTools.Field(typeof(DebugWindowsOpener), "widgetRow");
		CodeInstruction[] array = instructionsArr;
		foreach (CodeInstruction inst in array)
		{
			if (!patched && widgetRowField != null && inst.opcode == OpCodes.Bne_Un_S)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, widgetRowField);
				yield return new CodeInstruction(OpCodes.Call, new Action<WidgetRow>(QuickstartController.DrawDebugToolbarButton).Method);
				patched = true;
			}
			yield return inst;
		}
	}
}
