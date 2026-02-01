using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogFacialStuff : Window
{
	private Type facialUI = null;

	public override Vector2 InitialSize => new Vector2(256f, 65f);

	internal DialogFacialStuff()
	{
		try
		{
			facialUI = Reflect.GetAType("FacialStuff", "Harmony.HarmonyPatchesFS");
			facialUI.CallMethod("OpenStylingWindow", new object[1] { CEditor.API.Pawn });
		}
		catch
		{
		}
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		layer = CEditor.Layer;
	}

	public override void DoWindowContents(Rect inRect)
	{
		List<Window> windowOfStartsWithType = WindowTool.GetWindowOfStartsWithType("FacialStuff.FaceEditor.");
		WindowTool.BringToFrontMulti(windowOfStartsWithType);
		Window windowOfType = WindowTool.GetWindowOfType("FacialStuff.FaceEditor.Dialog_FaceStyling");
		if (windowOfType == null)
		{
			CEditor.API.UpdateGraphics();
			Close();
		}
	}
}
