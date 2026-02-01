using System;
using UnityEngine;
using Verse;

namespace CharacterEditor;

public class DialogPsychology : Window
{
	private Type psychologyUI = null;

	private Rect rect;

	private bool doOnce;

	public override Vector2 InitialSize => new Vector2(500f, WindowTool.MaxH);

	public DialogPsychology()
	{
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.Psychology);
		try
		{
			psychologyUI = Reflect.GetAType("Psychology", "PsycheCardUtility");
		}
		catch
		{
		}
		rect = new Rect(0f, 20f, 500f, 680f);
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
		doCloseX = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.Psychology, ref windowRect, ref doOnce, 0);
		}
		try
		{
			psychologyUI.CallMethod("DrawPsycheMenuCard", new object[2]
			{
				rect,
				CEditor.API.Pawn
			});
			psychologyUI.CallMethod("DrawDebugOptions", new object[2]
			{
				inRect,
				CEditor.API.Pawn
			});
			int index = WindowTool.GetIndex(this);
			WindowTool.TopLayerForWindowOfType("Psychology.Dialog_EditPsyche", force: false);
			SZWidgets.ButtonText(new Rect((float)((double)inRect.width * 0.600000023841858 - 84.0), 0f, 108f, 22f), "Modify", AModify, "changes will be applied after some passed time");
		}
		catch
		{
			Close();
		}
	}

	private void AModify()
	{
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.Psychology, windowRect.position);
		base.Close(doCloseSound);
	}
}
