using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class Gizmo_ActionAndToggle : Gizmo
{
	public Func<bool> isActive;

	public Action toggleAction;

	public Action mainAction;

	public string defaultLabel;

	public string defaultDesc;

	public Texture2D icon;

	public SoundDef turnOnSound = SoundDefOf.Checkbox_TurnedOn;

	public SoundDef turnOffSound = SoundDefOf.Checkbox_TurnedOff;

	public override float GetWidth(float maxWidth)
	{
		return 75f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		bool flag = Mouse.IsOver(rect);
		if (parms.highLight)
		{
			Widgets.DrawStrongHighlight(rect.ExpandedBy(4f));
		}
		Material material = (parms.lowLight ? TexUI.GrayscaleGUI : null);
		GUI.color = (parms.lowLight ? Command.LowLightBgColor : (flag ? GenUI.MouseoverColor : Color.white));
		GenUI.DrawTextureWithMaterial(rect, Command.BGTex, material);
		GUI.color = Color.white;
		Rect outerRect = new Rect(rect.x + 5f, rect.y + 5f, 64f, 64f);
		Texture tex = icon ?? BaseContent.BadTex;
		Widgets.DrawTextureFitted(outerRect, tex, 0.85f);
		string text = defaultLabel?.CapitalizeFirst() ?? "";
		if (!text.NullOrEmpty())
		{
			float num = Text.CalcHeight(text, rect.width);
			Rect rect2 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
			GUI.DrawTexture(rect2, (Texture)TexUI.GrayTextBG);
			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(rect2, text);
			Text.Anchor = TextAnchor.UpperLeft;
		}
		if (!defaultDesc.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, defaultDesc);
		}
		Rect rect3 = new Rect(rect.xMax - 24f, rect.y, 24f, 24f);
		Texture2D texture2D = (isActive() ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);
		GUI.DrawTexture(rect3, (Texture)texture2D);
		if (Widgets.ButtonInvisible(rect3))
		{
			toggleAction?.Invoke();
			(isActive() ? turnOffSound : turnOnSound)?.PlayOneShotOnCamera();
		}
		if (Widgets.ButtonInvisible(rect) && !rect3.Contains(Event.current.mousePosition))
		{
			mainAction?.Invoke();
			return new GizmoResult(GizmoState.Interacted, Event.current);
		}
		return flag ? new GizmoResult(GizmoState.Mouseover, Event.current) : new GizmoResult(GizmoState.Clear, null);
	}
}
