using System;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace MoreWidgets;

[HarmonyPatch]
public static class Patch_ShowCoords
{
	public static bool showMouse = false;

	public static bool showUI = false;

	private static string coordString;

	private static float height;

	private static int frame = -1;

	private static readonly Vector2 adjust = new Vector2(10f, 20f);

	private static string CoordString
	{
		get
		{
			if (Time.frameCount > frame)
			{
				frame = Time.frameCount;
				coordString = UI.MouseCell().ToString();
			}
			return coordString;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameConditionManager), "TotalHeightAt")]
	public static void TotalHeightAt_Post(float width, ref float __result, GameConditionManager __instance)
	{
		if (showUI && __instance.ownerMap != null)
		{
			height = Text.CalcHeight(CoordString, width - 6f);
			__result += height + 4f;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(GameConditionManager), "DoConditionsUI")]
	public static void DoConditionsUI_Pre(ref Rect rect, GameConditionManager __instance)
	{
		if (showUI && __instance.ownerMap != null)
		{
			Rect rect2 = rect.TopPartPixels(height);
			rect2.width -= 6f;
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.UpperRight;
			Widgets.Label(rect2, CoordString);
			Text.Anchor = anchor;
			rect.yMin = rect2.yMax + 4f;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameComponentUtility), "GameComponentOnGUI")]
	public static void DrawUI_Post()
	{
		if (showMouse && Find.CurrentMap != null)
		{
			Rect rect = new Rect(UI.MousePositionOnUIInverted + adjust, Text.CalcSize(CoordString));
			Widgets.DrawTextHighlight(rect, 4f, Color.black);
			Widgets.Label(rect, CoordString);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(DebugTabMenu_Settings), "InitActions")]
	public static void InitActions_Post(DebugActionNode __result)
	{
		AddSetting(__result, "Map coords in UI", delegate
		{
			showUI = !showUI;
		}, "showUI");
		AddSetting(__result, "Map coords at pointer", delegate
		{
			showMouse = !showMouse;
		}, "showMouse");
	}

	private static void AddSetting(DebugActionNode node, string label, Action action, string field)
	{
		node.AddChild(new DebugActionNode(label, DebugActionType.Action, action)
		{
			category = "View",
			settingsField = AccessTools.DeclaredField(typeof(Patch_ShowCoords), field)
		});
	}
}
