using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Gizmo_ApparelReloadableExtra : Gizmo
{
	private Comp_ApparelHediffAdder ApparelHediffAdder;

	private static readonly Texture2D FullBatteryBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.3f, 0.3f, 0.3f));

	private static readonly Texture2D EmptyBatteryBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

	public Gizmo_ApparelReloadableExtra(Comp_ApparelHediffAdder carrier)
	{
		ApparelHediffAdder = carrier;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(6f);
		Widgets.DrawWindowBackground(rect);
		Rect rect3 = rect2;
		rect3.height = rect.height / 2f;
		Text.Font = GameFont.Tiny;
		Widgets.Label(rect3, ApparelHediffAdder.parent.def.label.Translate().Resolve());
		Rect rect4 = rect2;
		rect4.yMin = rect2.y + rect2.height / 2f;
		float fillPercent = (float)ApparelHediffAdder.CompApparelReloadable.RemainingCharges / (float)ApparelHediffAdder.CompApparelReloadable.MaxCharges;
		Widgets.FillableBar(rect4, fillPercent, FullBatteryBarTex, EmptyBatteryBarTex, doBorder: true);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect4, ApparelHediffAdder.CompApparelReloadable.RemainingCharges + "/" + ApparelHediffAdder.CompApparelReloadable.MaxCharges);
		return new GizmoResult(GizmoState.Clear);
	}

	public override float GetWidth(float maxWidth)
	{
		return 140f;
	}
}
