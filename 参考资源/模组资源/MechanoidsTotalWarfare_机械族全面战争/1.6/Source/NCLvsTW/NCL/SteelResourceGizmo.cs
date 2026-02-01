using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCL;

[StaticConstructorOnStartup]
public class SteelResourceGizmo : Gizmo
{
	private readonly CompSteelResource comp;

	private float lastTargetValue;

	private float targetValue;

	private static bool draggingBar;

	private List<float> bandPercentages;

	private static readonly Texture2D ClearIcon = ContentFinder<Texture2D>.Get("UI/Icons/EjectContentFromAtomizer");

	private static readonly Texture2D BarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));

	private static readonly Texture2D BarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));

	private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

	private static readonly Texture2D DragBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

	private const int Increments = 24;

	public SteelResourceGizmo(CompSteelResource comp)
	{
		this.comp = comp;
		targetValue = (float)comp.maxToFill / (float)comp.Props.maxIngredientCount;
		bandPercentages = new List<float>();
		int maxIngredientCount = comp.Props.maxIngredientCount;
		if (maxIngredientCount >= 50)
		{
			int num = 50;
			int num2 = maxIngredientCount / num;
			for (int i = 0; i <= num2; i++)
			{
				float item = Mathf.Clamp01((float)(i * num) / (float)maxIngredientCount);
				bandPercentages.Add(item);
			}
		}
		else
		{
			int num3 = 12;
			for (int j = 0; j <= num3; j++)
			{
				bandPercentages.Add((float)j / (float)num3);
			}
		}
	}

	public override float GetWidth(float maxWidth)
	{
		return 160f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, 160f, 75f);
		Rect rect2 = rect.ContractedBy(10f);
		Widgets.DrawWindowBackground(rect);
		Text.Font = GameFont.Small;
		Rect clearButtonRect = new Rect(rect.x + rect.width - 25f, rect.y + 5f, 20f, 20f);
		if (Widgets.ButtonImage(clearButtonRect, ClearIcon, Color.white, Color.grey))
		{
			Find.WindowStack.Add(new Dialog_MessageBox("ConfirmClearSteel".Translate(comp.IngredientCount), "Confirm".Translate(), delegate
			{
				comp.EjectResources();
			}, "Cancel".Translate(), null, "ClearSteelTitle".Translate()));
			return new GizmoResult(GizmoState.Interacted);
		}
		TooltipHandler.TipRegion(clearButtonRect, "ClearSteelTooltip".Translate());
		string text = comp.Props.fixedIngredient.LabelCap;
		float height = Text.CalcHeight(text, rect2.width);
		Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, height);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect3, text);
		Text.Anchor = TextAnchor.UpperLeft;
		lastTargetValue = targetValue;
		float num = rect2.height - rect3.height;
		float num2 = num - 4f;
		float num3 = (num - num2) / 2f;
		Rect rect4 = new Rect(rect2.x, rect3.yMax + num3, rect2.width, num2);
		Widgets.DraggableBar(rect4, BarTex, BarHighlightTex, EmptyBarTex, DragBarTex, ref draggingBar, comp.FillPercentage, ref targetValue, bandPercentages, 24);
		Text.Anchor = TextAnchor.MiddleCenter;
		rect4.y -= 2f;
		string label = $"{comp.IngredientCount} / {comp.Props.maxIngredientCount} ";
		Widgets.Label(rect4, label);
		Text.Anchor = TextAnchor.UpperLeft;
		TooltipHandler.TipRegion(rect4, () => GetResourceBarTip(), Gen.HashCombineInt(comp.GetHashCode(), 34242369));
		if (lastTargetValue != targetValue)
		{
			comp.maxToFill = Mathf.RoundToInt(targetValue * (float)comp.Props.maxIngredientCount);
		}
		return new GizmoResult(GizmoState.Clear);
	}

	private string GetResourceBarTip()
	{
		return $"Steel Storage: {comp.IngredientCount}/{comp.Props.maxIngredientCount}\n" + $"Consumes {20} steel per missile";
	}
}
