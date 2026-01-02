using System;
using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class Dialog_MilianHairStyleConfig : Window
{
	public TaggedString text;

	public string title;

	public Pawn milian;

	public string buttonAText;

	private Rot4 portraitRot = Rot4.South;

	protected const float ButtonHeight = 35f;

	public Color colorSetting;

	private Color color;

	private Color colorOld;

	private Action<Color> action;

	private bool hsvColorWheelDragging;

	private string buffer_r;

	private string buffer_g;

	private string buffer_b;

	private Color refColor = Color.white;

	private Vector2 scrollPosition = Vector2.zero;

	public override Vector2 InitialSize => new Vector2(680f, 340f);

	public Dialog_MilianHairStyleConfig(Pawn milian, Color colorSetting, Action<Color> action, TaggedString text, string buttonAText = null, string title = null, WindowLayer layer = WindowLayer.Dialog)
	{
		this.text = text;
		this.buttonAText = buttonAText;
		this.title = title;
		base.layer = layer;
		this.milian = milian;
		this.colorSetting = colorSetting;
		color = colorSetting;
		colorOld = colorSetting;
		this.action = action;
		if (buttonAText.NullOrEmpty())
		{
			this.buttonAText = "OK".Translate();
		}
		forcePause = false;
		absorbInputAroundWindow = true;
		onlyOneOfTypeAllowed = true;
		closeOnCancel = true;
		closeOnAccept = true;
		doCloseX = true;
		doWindowBackground = true;
		closeOnClickedOutside = true;
		draggable = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Widgets.DrawLineVertical(230f, 0f, inRect.height);
		Rect rect = new Rect(inRect.x - 40f, inRect.y, 300f, 300f);
		Widgets.DrawWindowBackground(new Rect(inRect.x, inRect.y, 220f, inRect.height));
		DrawPortrait(milian, rect, 1.35f, ref portraitRot);
		string labelCap = milian.LabelCap;
		float height = Text.CalcHeight(labelCap, rect.width);
		Rect rect2 = new Rect(rect.x, rect.height - 20f, rect.width, height);
		Text.Anchor = TextAnchor.UpperCenter;
		Widgets.Label(rect2, labelCap);
		Text.Anchor = TextAnchor.UpperLeft;
		if (MiliraRaceSettings.MiliraRace_ModSetting_MilianHairColor && MiliraRaceSettings.MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride)
		{
			Widgets.DrawWindowBackground(new Rect(inRect.x + 240f, inRect.y, inRect.width - 240f, inRect.height));
			Rect rect3 = new Rect(inRect.x + 240f, inRect.y + inRect.height - 164f, 260f, 164f);
			DrawColorWheel(rect3);
			Rect rect4 = new Rect(inRect.x + 480f, inRect.y + inRect.height - 164f, 25f, 25f);
			if (Widgets.ButtonImage(rect4, TexButton.Copy))
			{
				MiliraGameComponent_OverallControl.OverallControl.colorClipboard = color;
				Messages.Message("Milira.MilianHairColor_CopySuccesss".Translate(milian.LabelCap), milian, MessageTypeDefOf.NeutralEvent, historical: false);
			}
			Rect rect5 = new Rect(inRect.x + 480f, inRect.y + inRect.height - 134f, 25f, 25f);
			MiliraGameComponent_OverallControl overallControl = MiliraGameComponent_OverallControl.OverallControl;
			if ((overallControl == null || overallControl.colorClipboard != default(Color)) && Widgets.ButtonImage(rect5, TexButton.Paste))
			{
				color = MiliraGameComponent_OverallControl.OverallControl.colorClipboard;
			}
			TooltipHandler.TipRegion(rect4, "Milira.MilianHairColor_Copy".Translate(milian.LabelCap));
			TooltipHandler.TipRegion(rect5, "Milira.MilianHairColor_Paste".Translate());
			if (Widgets.ButtonText(new Rect(inRect.x + 490f, inRect.y + inRect.height - 70f, 145f, 30f), "Reset".Translate()))
			{
				hsvColorWheelDragging = false;
				color = colorOld;
				milian?.Drawer?.renderer?.renderTree?.SetDirty();
				PortraitsCache.SetDirty(milian);
			}
			if (Widgets.ButtonText(new Rect(inRect.x + 490f, inRect.y + inRect.height - 35f, 145f, 30f), "Accept".Translate()))
			{
				hsvColorWheelDragging = false;
				action?.Invoke(color);
				milian?.Drawer?.renderer?.renderTree?.SetDirty();
				PortraitsCache.SetDirty(milian);
			}
		}
		DrawScrollHairSwitch(rect: new Rect(inRect.x + 250f, inRect.y + 10f, 380f, 120f), pawn: milian);
	}

	public void DrawScrollHairSwitch(Pawn pawn, Rect rect)
	{
		CompMilianHairSwitch compMilianHairSwitch = pawn.TryGetComp<CompMilianHairSwitch>();
		if (compMilianHairSwitch == null)
		{
			return;
		}
		List<string> frontHairPaths = compMilianHairSwitch.frontHairPaths;
		float width = (float)frontHairPaths.Count * 100f;
		Rect viewRect = new Rect(0f, 0f, width, rect.height - 20f);
		Widgets.ScrollHorizontal(rect, ref scrollPosition, viewRect);
		Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
		for (int i = 0; i < frontHairPaths.Count; i++)
		{
			string text = frontHairPaths[i];
			Texture2D texture2D = ContentFinder<Texture2D>.Get(text + "_south");
			if (texture2D != null)
			{
				Rect rect2 = new Rect((float)i * 100f, 0f, 90f, 90f);
				Rect outerRect = new Rect((float)i * 100f, -10f, 90f, 90f);
				GUI.DrawTexture(rect2, (Texture)Command.BGTex);
				Widgets.DrawTextureFitted(outerRect, texture2D, 1.2f);
				Widgets.DrawHighlightIfMouseover(rect2);
				if (Widgets.ButtonInvisible(rect2))
				{
					compMilianHairSwitch.ChangeGraphic(i);
					compMilianHairSwitch.num = i;
					PortraitsCache.SetDirty(pawn);
				}
			}
		}
		Widgets.EndScrollView();
	}

	public void DrawPortrait(Pawn pawn, Rect rect, float zoom, ref Rot4 rot)
	{
		RenderTexture renderTexture = PortraitsCache.Get(pawn, rect.size, rot, default(Vector3), zoom);
		Rect butRect = new Rect(rect.x + 50f, rect.height - 60f, 30f, 30f);
		Rect butRect2 = new Rect(rect.x + rect.width - 30f - 50f, rect.height - 60f, 30f, 30f);
		GUI.DrawTexture(rect, (Texture)renderTexture);
		if (Widgets.ButtonImageWithBG(butRect, AncotLibraryIcon.RotClockwise))
		{
			rot.Rotate(RotationDirection.Clockwise);
		}
		if (Widgets.ButtonImageWithBG(butRect2, AncotLibraryIcon.RotCounterclockwise))
		{
			rot.Rotate(RotationDirection.Counterclockwise);
		}
	}

	public void DrawColorWheel(Rect rect)
	{
		Rect rect2 = new Rect(rect.x, rect.y, 164f, 164f);
		Widgets.DrawAltRect(rect2);
		Widgets.HSVColorWheel(rect2, ref color, ref hsvColorWheelDragging, 1f);
		Rect rect3 = new Rect(rect.x + 180f, rect.y + 100f, 60f, 60f);
		Widgets.DrawBoxSolid(rect3, color);
		if (color != refColor)
		{
			buffer_r = color.r.ToString("0.##");
			buffer_g = color.g.ToString("0.##");
			buffer_b = color.b.ToString("0.##");
			refColor = color;
		}
		Rect rect4 = new Rect(rect.x + 130f, rect.y, rect.width - 150f, 30f);
		Rect rect5 = new Rect(rect.x + 130f, rect.y + 30f, rect.width - 150f, 30f);
		Rect rect6 = new Rect(rect.x + 130f, rect.y + 60f, rect.width - 150f, 30f);
		Widgets.TextFieldNumericLabeled(rect4, "R ", ref color.r, ref buffer_r, 0f, 1f);
		Widgets.TextFieldNumericLabeled(rect5, "G ", ref color.g, ref buffer_g, 0f, 1f);
		Widgets.TextFieldNumericLabeled(rect6, "B ", ref color.b, ref buffer_b, 0f, 1f);
	}
}
