using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;

namespace GD3
{
	[StaticConstructorOnStartup]
	public class HitArmorGizmo : Gizmo
	{
		private CompHitArmor carrier;

		private static readonly Texture2D BarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));

		private static readonly Texture2D BarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));

		private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

		private List<float> bandPercentages;

		public HitArmorGizmo(CompHitArmor carrier)
		{
			this.carrier = carrier;
			if (bandPercentages == null)
			{
				bandPercentages = new List<float>();
				int num = carrier.Props.limitOfTimes;
				for (int i = 0; i <= num; i++)
				{
					float item = 1f / (float)num * (float)i;
					bandPercentages.Add(item);
				}
			}
		}

		public override float GetWidth(float maxWidth)
		{
			return Math.Min(carrier.Props.limitOfTimes * 15f, 270f);
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			float percent = carrier.timesLeft / (float)carrier.Props.limitOfTimes;
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Rect rect2 = rect.ContractedBy(10f);
			Widgets.DrawWindowBackground(rect);
			Text.Font = GameFont.Small;
			TaggedString labelCap = "GD.HitArmorGizmoLabel".Translate();
			float height = Text.CalcHeight(labelCap, rect2.width);
			Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, height);
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect3, labelCap);
			Text.Anchor = TextAnchor.UpperLeft;
			float num = rect2.height - rect3.height;
			float num2 = num - 4f;
			float num3 = (num - num2) / 2f;
			Rect rect4 = new Rect(rect2.x, rect3.yMax + num3, rect2.width, num2);
			DraggableBar(rect4, BarTex, BarHighlightTex, EmptyBarTex, percent, bandPercentages);
			Text.Anchor = TextAnchor.MiddleCenter;
			rect4.y -= 2f;
			//Widgets.Label(rect4, carrier.IngredientCount.ToString() + " / " + carrier.Props.maxIngredientCount);
			Text.Anchor = TextAnchor.UpperLeft;
			TooltipHandler.TipRegion(rect4, () => GetResourceBarTip(), Gen.HashCombineInt(carrier.GetHashCode(), 34242369));
			return new GizmoResult(GizmoState.Clear);
		}

		private string GetResourceBarTip()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("GD.HitArmorBarTip".Translate(carrier.Props.limitOfTimes, carrier.timesLeft));
			return stringBuilder.ToString();
		}

		private void DraggableBar(Rect barRect, Texture2D barTexture, Texture2D barHighlightTexture, Texture2D emptyBarTex, float barValue, IEnumerable<float> bandPercentages = null)
		{
			bool flag = Mouse.IsOver(barRect);
			Widgets.FillableBar(barRect, Mathf.Min(barValue, 1f), flag ? barHighlightTexture : barTexture, emptyBarTex, doBorder: true);
			if (bandPercentages != null)
			{
				foreach (float bandPercentage in bandPercentages)
				{
					DrawDraggableBarThreshold(barRect, bandPercentage, barValue);
				}
			}
			
			GUI.color = Color.white;
		}

		private static void DrawDraggableBarThreshold(Rect rect, float percent, float curValue)
		{
			Rect rect2 = default(Rect);
			rect2.x = rect.x + 3f + (rect.width - 8f) * percent;
			rect2.y = rect.y + rect.height - 9f;
			rect2.width = 2f;
			rect2.height = 6f;
			Rect position = rect2;
			if (curValue < percent)
			{
				GUI.DrawTexture(position, BaseContent.GreyTex);
			}
			else
			{
				GUI.DrawTexture(position, BaseContent.BlackTex);
			}
		}
	}
}