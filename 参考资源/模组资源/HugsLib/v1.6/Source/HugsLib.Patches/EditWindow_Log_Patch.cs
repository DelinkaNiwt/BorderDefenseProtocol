using System;
using HarmonyLib;
using HugsLib.Logs;
using LudeonTK;
using UnityEngine;
using Verse;

namespace HugsLib.Patches;

/// <summary>
/// Adds extra buttons to the Log window.
/// </summary>
[HarmonyPatch(typeof(EditWindow_Log))]
[HarmonyPatch("DoMessagesListing")]
[HarmonyPatch(new Type[] { typeof(Rect) })]
internal static class EditWindow_Log_Patch
{
	[HarmonyPrefix]
	private static bool ExtraLogWindowButtons(Window __instance, ref Rect listingRect)
	{
		Rect inRect = new Rect(listingRect);
		listingRect.yMax -= LogWindowExtensions.ExtensionsAreaHeight;
		inRect.yMin = listingRect.yMax;
		LogWindowExtensions.DrawLogWindowExtensions(__instance, inRect);
		return true;
	}
}
