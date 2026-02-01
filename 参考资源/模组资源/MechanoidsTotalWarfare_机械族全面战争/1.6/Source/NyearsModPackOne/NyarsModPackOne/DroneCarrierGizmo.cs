using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace NyarsModPackOne;

[StaticConstructorOnStartup]
public class DroneCarrierGizmo : Gizmo
{
	private CompDroneCarrier carrier;

	private float lastTargetValue;

	private float targetValue;

	private static bool draggingBar;

	private List<float> bandPercentages;

	private static readonly Texture2D BarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));

	private static readonly Texture2D BarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));

	private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

	private static readonly Texture2D DragBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

	private const int Increments = 24;

	private const float Width = 160f;

	public DroneCarrierGizmo(CompDroneCarrier carrier)
	{
		this.carrier = carrier;
		targetValue = (float)carrier.maxToFill / (float)carrier.Props.maxIngredientCount;
		bandPercentages = new List<float>();
		int maxIngredientCount = carrier.Props.maxIngredientCount;
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
			bandPercentages = new List<float>();
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
		string text = carrier.Props.fixedIngredient.LabelCap;
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
		Widgets.DraggableBar(rect4, BarTex, BarHighlightTex, EmptyBarTex, DragBarTex, ref draggingBar, carrier.FillPercentage, ref targetValue, bandPercentages, 24);
		Text.Anchor = TextAnchor.MiddleCenter;
		rect4.y -= 2f;
		string label = $"{carrier.IngredientCount} / {carrier.Props.maxIngredientCount} ";
		Widgets.Label(rect4, label);
		Text.Anchor = TextAnchor.UpperLeft;
		TooltipHandler.TipRegion(rect4, () => GetResourceBarTip(), Gen.HashCombineInt(carrier.GetHashCode(), 34242369));
		if (lastTargetValue != targetValue)
		{
			carrier.maxToFill = Mathf.RoundToInt(targetValue * (float)carrier.Props.maxIngredientCount);
		}
		return new GizmoResult(GizmoState.Clear);
	}

	private string GetResourceBarTip()
	{
		StringBuilder stringBuilder = new StringBuilder();
		return stringBuilder.ToString();
	}

	private void DrawTickMarks(Rect rect)
	{
		GUI.color = new Color(1f, 1f, 1f, 0.5f);
		float length = 5f;
		foreach (float bandPercentage in bandPercentages)
		{
			if (bandPercentage > 0f && bandPercentage < 1f)
			{
				float x = rect.x + rect.width * bandPercentage;
				Widgets.DrawLineVertical(x, rect.y, length);
			}
		}
		GUI.color = Color.white;
	}
}
