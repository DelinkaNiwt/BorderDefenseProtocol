using System;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class CustomDialog : Window
{
	internal string text;

	internal string title;

	internal Action buttonAbortAction;

	internal Action buttonAcceptAction;

	internal Action buttonNextAction;

	private Vector2 scrollPosition = Vector2.zero;

	private float creationRealTime = -1f;

	private const float TitleHeight = 42f;

	protected const float ButtonHeight = 35f;

	public override Vector2 InitialSize => new Vector2(640f, 460f);

	internal CustomDialog(string text, string title, Action onAbort, Action onConfirm, Action onNext)
	{
		this.title = title;
		this.text = text;
		buttonAbortAction = onAbort;
		buttonAcceptAction = onConfirm;
		buttonNextAction = onNext;
		layer = CEditor.Layer;
		forcePause = true;
		absorbInputAroundWindow = true;
		creationRealTime = RealTime.LastRealTime;
		onlyOneOfTypeAllowed = false;
		closeOnAccept = true;
		closeOnCancel = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		float num = inRect.y;
		if (!title.NullOrEmpty())
		{
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(0f, num, inRect.width, 42f), title);
			num += 42f;
		}
		Text.Font = GameFont.Small;
		Rect outRect = new Rect(inRect.x, num, inRect.width, (float)((double)inRect.height - 35.0 - 5.0) - num);
		float width = outRect.width - 16f;
		Rect viewRect = new Rect(0f, 0f, width, Text.CalcHeight(text, width));
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), text);
		Widgets.EndScrollView();
		GUI.color = Color.white;
		float num2 = InitialSize.y - 70f;
		SZWidgets.CheckBoxOnChange(new Rect(inRect.x + 420f, num2 - 30f, 180f, 30f), Label.ALWAYS_SKIP, CEditor.DontAsk, ASetAlwaysSkip);
		SZWidgets.ButtonText(new Rect(inRect.x, num2, 180f, 30f), "Cancel".Translate(), AOnAbort);
		SZWidgets.ButtonText(new Rect(inRect.x + (float)((buttonNextAction == null) ? 420 : 210), num2, 180f, 30f), "Confirm".Translate(), AOnAccept);
		if (buttonNextAction != null)
		{
			SZWidgets.ButtonText(new Rect(inRect.x + 420f, num2, 180f, 30f), Label.SKIP, AOnNext);
		}
	}

	private void ASetAlwaysSkip(bool val)
	{
		CEditor.DontAsk = val;
	}

	private void AOnAbort()
	{
		if (buttonAbortAction != null)
		{
			buttonAbortAction();
		}
		Close();
	}

	private void AOnAccept()
	{
		if (buttonAcceptAction != null)
		{
			buttonAcceptAction();
		}
		Close();
	}

	private void AOnNext()
	{
		if (buttonNextAction != null)
		{
			buttonNextAction();
		}
		Close();
	}

	public override void OnCancelKeyPressed()
	{
		AOnAbort();
	}

	public override void OnAcceptKeyPressed()
	{
		AOnAccept();
	}
}
