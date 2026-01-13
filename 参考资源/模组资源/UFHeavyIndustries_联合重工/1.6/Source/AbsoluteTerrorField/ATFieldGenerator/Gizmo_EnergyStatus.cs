using UnityEngine;
using Verse;

namespace ATFieldGenerator;

[StaticConstructorOnStartup]
public class Gizmo_EnergyStatus : Gizmo
{
	public Comp_AbsoluteTerrorField comp;

	private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.45f, 0.8f));

	private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

	public Gizmo_EnergyStatus(Comp_AbsoluteTerrorField comp)
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
		Widgets.DrawWindowBackground(rect);
		Rect rect2 = rect.ContractedBy(6f);
		Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, 25f);
		Text.Font = GameFont.Small;
		string text = "ATField_Energy_Label".Translate();
		float x = Text.CalcSize(text).x;
		Rect rect4 = new Rect(rect3.x + (rect3.width - x) / 2f, rect3.y, x, rect3.height);
		Widgets.Label(rect4, text);
		Rect rect5 = new Rect(rect2.x, rect2.y + 30f, rect2.width, rect2.height - 30f);
		if (comp.State == ShieldState.Resetting)
		{
			Widgets.FillableBar(rect5, 0f, FullBarTex, BaseContent.BlackTex, doBorder: false);
			string text2 = "ATField_Rebooting".Translate(((float)comp.ticksToReset / 60f).ToString("F1"));
			Vector2 vector = Text.CalcSize(text2);
			Rect rect6 = new Rect(rect5.x + (rect5.width - vector.x) / 2f, rect5.y + (rect5.height - vector.y) / 2f, vector.x, vector.y);
			Widgets.Label(rect6, text2);
		}
		else
		{
			float fillPercent = comp.energy / (float)comp.EnergyMax;
			Widgets.FillableBar(rect5, fillPercent, FullBarTex, BaseContent.BlackTex, doBorder: false);
			string text3 = comp.energy.ToString("F0") + " / " + comp.EnergyMax.ToString("F0");
			Vector2 vector2 = Text.CalcSize(text3);
			Rect rect7 = new Rect(rect5.x + (rect5.width - vector2.x) / 2f, rect5.y + (rect5.height - vector2.y) / 2f, vector2.x, vector2.y);
			Widgets.Label(rect7, text3);
		}
		return new GizmoResult(GizmoState.Clear);
	}
}
