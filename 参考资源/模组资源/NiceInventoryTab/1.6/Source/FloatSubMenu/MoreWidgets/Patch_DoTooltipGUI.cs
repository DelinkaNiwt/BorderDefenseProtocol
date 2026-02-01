using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MoreWidgets;

[HarmonyPatch]
public static class Patch_DoTooltipGUI
{
	[HarmonyTargetMethods]
	public static IEnumerable<MethodInfo> Targets()
	{
		yield return AccessTools.Method(typeof(LongEventHandler), "LongEventsOnGUI");
		yield return AccessTools.Method(typeof(UIRoot), "UIRootOnGUI");
	}

	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> orig)
	{
		MethodInfo method = AccessTools.Method(typeof(TooltipHandler), "DoTooltipGUI");
		foreach (CodeInstruction instr in orig)
		{
			yield return instr;
			if (instr.Calls(method))
			{
				yield return CodeInstruction.Call(typeof(TooltipHandler2), "DoTooltipGUI");
			}
		}
	}
}
