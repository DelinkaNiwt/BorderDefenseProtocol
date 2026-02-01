using HarmonyLib;
using UnityEngine;
using Verse;

namespace FloatSubMenus;

public class FloatMenuDivider : FloatMenuOption
{
	private readonly string label;

	private readonly Traverse<float> widthField;

	private readonly Traverse<float> heightField;

	private readonly float labelWidth;

	private readonly Vector2 size;

	private const float HorizMargin = 3f;

	private const float VertMargin = 1f;

	private const float MinLineLength = 10f;

	private const float MinWidth = 100f;

	private const float MaxTextWidth = 294f;

	public FloatMenuDivider(string label = null)
		: base(" ", NoAction)
	{
		this.label = label;
		Traverse traverse = Traverse.Create(this);
		widthField = traverse.Field<float>("cachedRequiredWidth");
		heightField = traverse.Field<float>("cachedRequiredHeight");
		GameFont font = Text.Font;
		Text.Font = GameFont.Tiny;
		labelWidth = Text.CalcSize(label).x;
		Text.CalcHeight(label, 294f);
		size.x = Mathf.Min(labelWidth + 6f, 100f);
		size.y = 2f + Text.CalcHeight(base.Label, 294f);
		Text.Font = font;
		SetupSize();
	}

	private static void NoAction()
	{
	}

	private void SetupSize()
	{
		widthField.Value = size.x;
		heightField.Value = size.y;
	}

	public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
	{
		SetupSize();
		Text.Font = GameFont.Tiny;
		if (tooltip.HasValue)
		{
			TooltipHandler.TipRegion(rect, tooltip.Value);
		}
		Color color = GUI.color;
		GUI.color = FloatMenuOption.ColorBGActive * color;
		GUI.DrawTexture(rect, (Texture)BaseContent.WhiteTex);
		GUI.color = FloatMenuOption.ColorTextDisabled * color;
		Widgets.DrawAtlas(rect, TexUI.FloatMenuOptionBG);
		GUI.color = FloatMenuOption.ColorTextDisabled * color * 0.75f;
		Rect rect2 = rect.ContractedBy(3f, 1f);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect2, label);
		Text.Anchor = TextAnchor.UpperLeft;
		float num = rect2.width - labelWidth - 3f;
		if (num > 10f)
		{
			Widgets.DrawLineHorizontal(rect2.x + labelWidth + 3f, Mathf.Round(rect2.y + 0.65f * rect2.height), num);
		}
		GUI.color = color;
		return false;
	}
}
