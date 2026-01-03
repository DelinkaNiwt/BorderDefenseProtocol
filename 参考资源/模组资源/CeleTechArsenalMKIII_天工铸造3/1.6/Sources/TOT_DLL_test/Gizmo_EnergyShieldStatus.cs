using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Gizmo_EnergyShieldStatus : Gizmo
{
	public Comp_CMCShield shield;

	private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0.5f, 0.99f));

	private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

	public Gizmo_EnergyShieldStatus()
	{
		Order = -100f;
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
		Rect rect3 = rect2;
		rect3.height = rect.height / 2f;
		Text.Font = GameFont.Tiny;
		Widgets.Label(rect3, shield.IsApparel ? shield.parent.LabelCap : "ShieldInbuilt".Translate().Resolve());
		Rect rect4 = rect2;
		rect4.yMin = rect2.y + rect2.height / 2f;
		float fillPercent = shield.Energy / Mathf.Max(1f, shield.parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax));
		Widgets.FillableBar(rect4, fillPercent, FullShieldBarTex, EmptyShieldBarTex, doBorder: false);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect4, (shield.Energy * 100f).ToString("F0") + " / " + (shield.parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax) * 100f).ToString("F0"));
		Text.Anchor = TextAnchor.UpperLeft;
		TooltipHandler.TipRegion(rect2, "ShieldPersonalTip".Translate());
		return new GizmoResult(GizmoState.Clear);
	}
}
