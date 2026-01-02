using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class Gizmo_ApparelReloadable_Custom : Gizmo
{
	private CompApparelReloadable_Custom compReloadable;

	private const float Width = 160f;

	private static readonly Texture2D BarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));

	private static readonly Texture2D BarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));

	private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

	private static readonly Texture2D DragBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

	private const int Increments = 24;

	private static bool draggingBar;

	private float lastTargetValue;

	private float targetValue;

	private static List<float> bandPercentages;

	public Gizmo_ApparelReloadable_Custom(CompApparelReloadable_Custom compReloadable)
	{
		this.compReloadable = compReloadable;
		targetValue = (float)compReloadable.targetCharges / (float)compReloadable.MaxAmmoAmount();
		if (bandPercentages == null)
		{
			bandPercentages = new List<float>();
			int num = 20;
			for (int i = 0; i <= num; i++)
			{
				float item = 1f / (float)num * (float)i;
				bandPercentages.Add(item);
			}
		}
	}

	public override float GetWidth(float maxWidth)
	{
		return 160f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(10f);
		Widgets.DrawWindowBackground(rect);
		Text.Font = GameFont.Small;
		TaggedString taggedString = compReloadable.AmmoDef.label;
		float height = Text.CalcHeight(taggedString, rect2.width);
		Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, height);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect3, taggedString);
		Text.Anchor = TextAnchor.UpperLeft;
		lastTargetValue = targetValue;
		float num = rect2.height - rect3.height;
		float num2 = num - 4f;
		float num3 = (num - num2) / 2f;
		Rect rect4 = new Rect(rect2.x, rect3.yMax + num3, rect2.width, num2);
		if (compReloadable.Props.maxReloadConfigurable)
		{
			Widgets.DraggableBar(rect4, BarTex, BarHighlightTex, EmptyBarTex, DragBarTex, ref draggingBar, compReloadable.PercentageFull, ref targetValue, bandPercentages, 50);
		}
		else
		{
			Widgets.FillableBar(rect4, compReloadable.PercentageFull, BarTex, EmptyBarTex, doBorder: true);
		}
		Text.Anchor = TextAnchor.MiddleCenter;
		rect4.y -= 2f;
		Widgets.Label(rect4, compReloadable.LabelRemaining);
		Text.Anchor = TextAnchor.UpperLeft;
		TooltipHandler.TipRegion(rect4, () => GetResourceBarTip(), Gen.HashCombineInt(compReloadable.parent.GetHashCode(), 41546752));
		if (lastTargetValue != targetValue)
		{
			compReloadable.targetCharges = Mathf.RoundToInt(targetValue * (float)compReloadable.MaxCharges);
		}
		return new GizmoResult(GizmoState.Clear);
	}

	protected string GetResourceBarTip()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(string.Concat("Ancot.ApparelReloadableDesc".Translate() + "\n\n" + compReloadable.AmmoDef.label + ": ", compReloadable.targetCharges.ToString()));
		return stringBuilder.ToString();
	}
}
