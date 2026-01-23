using System;
using UnityEngine;
using Verse;

namespace HugsLib.Utils;

/// <summary>
/// A compact confirm dialog with Esc and Enter key support.
/// </summary>
public class Dialog_Confirm : Dialog_MessageBox
{
	private const float TitleHeight = 42f;

	private const float DialogWidth = 500f;

	private const float DialogHeight = 300f;

	public override Vector2 InitialSize
	{
		get
		{
			float num = 300f;
			if (title != null)
			{
				num += 42f;
			}
			return new Vector2(500f, num);
		}
	}

	public Dialog_Confirm(string text, Action confirmedAct = null, bool destructive = false, string title = null)
		: base(text, "Confirm".Translate(), confirmedAct, "GoBack".Translate(), null, title, destructive)
	{
		closeOnCancel = false;
		closeOnAccept = false;
	}

	public override void DoWindowContents(Rect inRect)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		base.DoWindowContents(inRect);
		if ((int)Event.current.type != 4)
		{
			return;
		}
		if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
		{
			if (buttonAAction != null)
			{
				buttonAAction();
			}
			Close();
		}
		else if (Event.current.keyCode == KeyCode.Escape)
		{
			if (buttonBAction != null)
			{
				buttonBAction();
			}
			Close();
		}
	}
}
