using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class StatBar : StatDrawer
{
	public class Overlay
	{
		public float Value;

		public Color Color;

		public Color? ColorBG;

		public Texture2D Tiled;

		public Vector2 TiledMove = new Vector2(8.325f, -4.59f);

		public float FadeRatio = 1f;

		public Overlay(float v, Color c, float fade = 1f)
		{
			Value = v;
			Color = c;
			FadeRatio = fade;
		}

		public void SetTiled(Texture2D t, Vector2? m = null, Color? bg = null)
		{
			Tiled = t;
			if (m.HasValue)
			{
				TiledMove = m.Value;
			}
			ColorBG = bg;
		}
	}

	public Func<Pawn, float> StatWorkerMax = (Pawn _p) => 10f;

	public Color ColorBar;

	public Texture2D Icon;

	public FloatRef TextSep;

	public FloatRef DigitSep;

	public bool Overbuffed;

	public float MaxValue = 1f;

	private List<Overlay> Overlays = new List<Overlay>();

	private float HighlightRatio;

	public static float PixelPerfectOffset => 0f;

	public StatBar(string title, string descr, Func<Pawn, StatDrawer, float> v_worker, Func<Pawn, float> max_worker, Assets.IconColor ic, FloatRef tsep = null, FloatRef digitSep = null)
	{
		Title = title;
		Descr = descr;
		StatWorker = v_worker;
		StatWorkerMax = max_worker;
		SetFixedHeight(StatDrawer.StatFixedBaseHeight);
		Icon = ic.Icon;
		ColorBar = ic.Color;
		TextSep = tsep ?? new FloatRef();
		DigitSep = digitSep ?? new FloatRef();
	}

	public override void UpdateValues(Pawn pawn)
	{
		ClearOverlays();
		Value = StatWorker(pawn, this);
		MaxValue = Mathf.Max(StatWorkerMax(pawn), Value);
	}

	public Overlay AddOverlay(Overlay o)
	{
		if (o.Value != 0f)
		{
			Overlays.Insert(0, o);
		}
		return o;
	}

	public void AddDebuff(float v, Color cl)
	{
		if (v < 0f && Settings.DebuffVisible)
		{
			AddOverlay(v, cl).SetTiled(Assets.DiagTiledTex, Vector2.zero);
		}
	}

	public void AddBuff(float v, Color cl)
	{
		if (v > 0f)
		{
			cl = Settings.ColorCorrect(cl);
			AddOverlay(v, cl).SetTiled(Assets.BuffTiledTex, null, Color.Lerp(cl, Assets.HediffBuffColor, 0.5f));
		}
	}

	public void AddAutoBuffDebuff(float v, Color barColor, Color? penaltyCustom = null)
	{
		if (v < 0f)
		{
			AddDebuff(v, penaltyCustom ?? Assets.PenaltyColor);
		}
		else
		{
			AddBuff(v, barColor);
		}
	}

	public Overlay AddOverlay(float v, Color cl, float fade = 1f)
	{
		return AddOverlay(new Overlay(v, cl, fade));
	}

	public void ClearOverlays()
	{
		Overlays.Clear();
	}

	public override void Draw()
	{
		string text = null;
		if (Mouse.IsOver(Geometry))
		{
			HighlightRatio = Mathf.Min(HighlightRatio + 0.01f, 1f);
			text = Descr;
			Assets.DrawHighlightHorizontal(Geometry.ContractedBy(0f, -2f), 0.2f);
		}
		else
		{
			HighlightRatio = Mathf.Max(HighlightRatio - 0.01f, 0f);
		}
		Text.Font = GameFont.Small;
		string finalValue = FinalValue;
		float leftPart_pix = TextSep.Comp(Utils.CalcWidth(Title));
		float rightPart_pix = DigitSep.Comp(Utils.CalcWidth(finalValue) + 8f);
		(Rect left, Rect right) tuple = Utils.SplitRectByLeftPart(Geometry, leftPart_pix, 20f);
		Rect item = tuple.left;
		Rect item2 = tuple.right;
		item2.y = Geometry.center.y - 10f;
		item2.height = 20f;
		(Rect left, Rect right) tuple2 = Utils.SplitRectByRightPart(item2, rightPart_pix, 6f);
		Rect item3 = tuple2.left;
		Rect item4 = tuple2.right;
		item2 = item3;
		Rect rect = item2.ContractedBy(4f);
		rect.xMin += 6f;
		Widgets.DrawBoxSolid(item4, Assets.ColorBG);
		Text.Anchor = TextAnchor.MiddleRight;
		GUI.color = Assets.ColorStat;
		Widgets.Label(item, Title);
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(item4, finalValue);
		Text.Anchor = TextAnchor.UpperLeft;
		Widgets.DrawBoxSolid(item2, Assets.ColorBG);
		DrawBar(rect);
		Rect rect2 = rect;
		float num = rect2.width / 6f;
		int num2 = Mathf.FloorToInt(rect2.width / num);
		GUI.color = Assets.ColorBG;
		for (int i = 1; i < num2; i++)
		{
			GUI.DrawTexture(new Rect(rect2.x + (float)i * num, rect2.y + rect2.height / 2f, 2f, rect2.height / 2f), (Texture)BaseContent.WhiteTex);
		}
		if (Icon != null)
		{
			Vector2 center = new Vector2(item.xMax + 20f, Geometry.center.y);
			Assets.DrawDiamond(center, 45f, Assets.ColorBG);
			GUI.color = Color.white;
			GUI.DrawTexture(Utils.RectCentered(center.x, center.y + PixelPerfectOffset, 16f), (Texture)Icon);
		}
		if (!text.NullOrEmpty())
		{
			TooltipHandler.TipRegion(Geometry, text);
		}
	}

	public virtual void DrawBar(Rect bar)
	{
		float num = Value;
		foreach (Overlay overlay in Overlays)
		{
			if (overlay.Value < 0f)
			{
				num -= overlay.Value;
			}
		}
		float num2 = num / MaxValue;
		if (num2 <= 0f)
		{
			return;
		}
		Rect rect = bar.LeftPart(num2);
		Color color = Settings.ColorCorrect(ColorBar);
		foreach (Overlay overlay2 in Overlays)
		{
			if (rect.width <= 0f)
			{
				return;
			}
			float rightPart_pix = Mathf.Max(Mathf.Abs(overlay2.Value) / MaxValue * bar.width, 5f);
			(Rect left, Rect right) tuple = Utils.SplitRectByRightPart(rect, rightPart_pix, 2f);
			Rect item = tuple.left;
			Rect item2 = tuple.right;
			rect = item;
			item2.xMax = Mathf.Min(item2.xMax, bar.xMax);
			if (overlay2.Tiled != null)
			{
				if (overlay2.ColorBG.HasValue)
				{
					Widgets.DrawBoxSolid(item2, overlay2.ColorBG.Value);
				}
				GUI.color = Color.Lerp(color, overlay2.Color, Mathf.Max(overlay2.FadeRatio, HighlightRatio));
				float num3 = Time.time * 2f;
				Assets.DrawTilingTexture(item2, overlay2.Tiled, 64f, item2.position + overlay2.TiledMove * num3);
			}
			else
			{
				Widgets.DrawBoxSolid(item2, Color.Lerp(color, overlay2.Color, Mathf.Max(overlay2.FadeRatio, HighlightRatio)));
			}
		}
		if (!(rect.width <= 0f))
		{
			Widgets.DrawBoxSolid(rect, color);
			if (Overbuffed)
			{
				float num4 = Time.time * 2f;
				GUI.color = Color.Lerp(color, Color.black, 0.15f);
				Assets.DrawTilingTexture(rect, Assets.BuffTiledTex, 64f, rect.position + new Vector2(8.325f, -4.59f) * num4);
			}
		}
	}
}
