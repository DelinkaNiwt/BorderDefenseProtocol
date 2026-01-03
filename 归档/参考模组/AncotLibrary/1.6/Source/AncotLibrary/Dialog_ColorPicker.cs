using System;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Dialog_ColorPicker : Window
{
	protected static readonly Vector2 ButSize = new Vector2(120f, 30f);

	public Color colorSetting;

	private Color color;

	private Color colorOld;

	private Action<Color> action;

	private bool hsvColorWheelDragging;

	private string buffer_r;

	private string buffer_g;

	private string buffer_b;

	private Color refColor = Color.white;

	public override Vector2 InitialSize => new Vector2(300f, 250f);

	public Dialog_ColorPicker(Color colorSetting, Action<Color> action, WindowLayer layer = WindowLayer.Dialog)
	{
		this.colorSetting = colorSetting;
		color = colorSetting;
		colorOld = colorSetting;
		this.action = action;
		forcePause = true;
		absorbInputAroundWindow = true;
		onlyOneOfTypeAllowed = true;
		closeOnCancel = true;
		closeOnAccept = true;
		doWindowBackground = true;
		draggable = true;
		closeOnClickedOutside = true;
		doCloseX = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)Event.current.type != 8)
		{
			Rect rect = new Rect(inRect.x, inRect.y, 164f, 164f);
			Widgets.DrawAltRect(rect);
			Widgets.HSVColorWheel(rect, ref color, ref hsvColorWheelDragging, 1f);
			Rect rect2 = new Rect(inRect.x + 180f, inRect.y + 100f, 60f, 60f);
			Widgets.DrawBoxSolid(rect2, color);
			if (color != refColor)
			{
				buffer_r = color.r.ToString("0.##");
				buffer_g = color.g.ToString("0.##");
				buffer_b = color.b.ToString("0.##");
				refColor = color;
			}
			Rect rect3 = new Rect(inRect.x + 130f, inRect.y, inRect.width - 150f, 30f);
			Rect rect4 = new Rect(inRect.x + 130f, inRect.y + 30f, inRect.width - 150f, 30f);
			Rect rect5 = new Rect(inRect.x + 130f, inRect.y + 60f, inRect.width - 150f, 30f);
			Widgets.TextFieldNumericLabeled(rect3, "R ", ref color.r, ref buffer_r, 0f, 1f);
			Widgets.TextFieldNumericLabeled(rect4, "G ", ref color.g, ref buffer_g, 0f, 1f);
			Widgets.TextFieldNumericLabeled(rect5, "B ", ref color.b, ref buffer_b, 0f, 1f);
			RectDivider layout = new RectDivider(inRect, 195953069);
			BottomButtons(ref layout);
		}
	}

	private void BottomButtons(ref RectDivider layout)
	{
		RectDivider rectDivider = layout.NewRow(ButSize.y, VerticalJustification.Bottom);
		if (Widgets.ButtonText(rectDivider.NewCol(ButSize.x), "Reset".Translate()))
		{
			hsvColorWheelDragging = false;
			color = colorOld;
		}
		if (Widgets.ButtonText(rectDivider.NewCol(ButSize.x, HorizontalJustification.Right), "Accept".Translate()))
		{
			hsvColorWheelDragging = false;
			action?.Invoke(color);
			Close();
		}
	}
}
