using System;
using HarmonyLib;
using Verse;

namespace HugsLib.Patches;

/// <summary>
/// Hooks into the flow of the vanilla MonoBehavior.OnGUI().
/// Unlike the <see cref="T:HugsLib.Patches.UIRoot_OnGUI_Patch" /> patch, this hook also
/// allows to receive OnGUI events during loading screens. 
/// </summary>
[HarmonyPatch(typeof(Root))]
[HarmonyPatch("OnGUI")]
[HarmonyPatch(new Type[] { })]
internal static class Root_OnGUI_Patch
{
	[HarmonyPostfix]
	private static void OnGUIHookUnfiltered()
	{
		HugsLibController.Instance.OnGUIUnfiltered();
	}
}
