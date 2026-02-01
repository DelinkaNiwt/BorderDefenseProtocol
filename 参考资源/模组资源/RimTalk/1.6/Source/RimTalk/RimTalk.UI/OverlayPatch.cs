using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalk.UI;

[HarmonyPatch(typeof(UIRoot_Play), "UIRootOnGUI")]
public static class OverlayPatch
{
	private static bool _skip;

	private static void DrawOverlay(bool isPrefixExecution)
	{
		_skip = !_skip;
		if (!_skip && Current.ProgramState == ProgramState.Playing)
		{
			RimTalkSettings settings = Settings.Get();
			if (settings.OverlayDrawAboveUI != isPrefixExecution)
			{
				(Find.CurrentMap?.GetComponent<Overlay>())?.MapComponentOnGUI();
			}
		}
	}

	[HarmonyPrefix]
	public static void Prefix()
	{
		DrawOverlay(isPrefixExecution: true);
	}

	[HarmonyPostfix]
	public static void Postfix()
	{
		DrawOverlay(isPrefixExecution: false);
	}
}
