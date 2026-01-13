using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HarmonyMod;

[HarmonyPatch(typeof(VersionControl), "DrawInfoInCorner")]
internal static class VersionControl_DrawInfoInCorner_Patch
{
	public static void Postfix()
	{
		string text = $"Harmony v{Main.loadedHarmonyVersion}";
		Text.Font = GameFont.Small;
		GUI.color = Color.white.ToTransparent(0.5f);
		Vector2 vector = Text.CalcSize(text);
		Rect rect = new Rect(10f, 58f, vector.x, vector.y);
		Widgets.Label(rect, text);
		GUI.color = Color.white;
		if (Mouse.IsOver(rect))
		{
			TipSignal tip = new TipSignal("Harmony Mod v" + Main.modVersion);
			TooltipHandler.TipRegion(rect, tip);
			Widgets.DrawHighlight(rect);
		}
		if (Main.loadingError != null)
		{
			Find.WindowStack.Add(new Dialog_MessageBox(Main.loadingError, "OK"));
			Main.loadingError = null;
		}
	}
}
