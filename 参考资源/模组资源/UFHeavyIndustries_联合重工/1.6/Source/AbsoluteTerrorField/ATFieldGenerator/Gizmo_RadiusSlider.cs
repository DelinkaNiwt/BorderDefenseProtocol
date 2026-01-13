using UnityEngine;
using Verse;

namespace ATFieldGenerator;

[StaticConstructorOnStartup]
public class Gizmo_RadiusSlider : Gizmo
{
	public Comp_AbsoluteTerrorField comp;

	private static readonly Texture2D BarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.45f, 0.8f));

	private static readonly Texture2D BGTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.15f, 0.15f, 0.15f));

	public Gizmo_RadiusSlider(Comp_AbsoluteTerrorField comp)
	{
		this.comp = comp;
	}

	public override float GetWidth(float maxWidth)
	{
		return 140f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(6f);
		Widgets.DrawWindowBackground(rect);
		Text.Font = GameFont.Small;
		Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, rect2.height / 2f);
		Widgets.Label(rect3, "ATField_Radius_Label".Translate(comp.radius.ToString("F0")));
		Rect rect4 = new Rect(rect2.x, rect2.y + rect2.height / 2f + 2f, rect2.width, 20f);
		float f = DrawCustomSlider(rect4, comp.radius, comp.minRadius, comp.maxRadius);
		comp.radius = Mathf.Round(f);
		return new GizmoResult(GizmoState.Clear);
	}

	private float DrawCustomSlider(Rect rect, float value, float min, float max)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		if (Mouse.IsOver(rect) || Mouse.IsOver(rect.ExpandedBy(5f)))
		{
			Event current = Event.current;
			if (((int)current.type == 0 || (int)current.type == 3) && current.button == 0)
			{
				float num = Mathf.Clamp01((current.mousePosition.x - rect.x) / rect.width);
				value = min + (max - min) * num;
				current.Use();
			}
		}
		GUI.DrawTexture(rect, (Texture)BGTex);
		float num2 = Mathf.Clamp01((value - min) / (max - min));
		Rect rect2 = new Rect(rect.x, rect.y, rect.width * num2, rect.height);
		GUI.DrawTexture(rect2, (Texture)BarTex);
		Widgets.DrawBox(rect);
		return value;
	}
}
