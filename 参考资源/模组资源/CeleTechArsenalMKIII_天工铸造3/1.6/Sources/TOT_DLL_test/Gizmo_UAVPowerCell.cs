using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Gizmo_UAVPowerCell : Gizmo
{
	public Comp_FloatingGunRework comp_FloatingGunRework;

	private static readonly Texture2D FullBatteryBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.7f, 0.7f));

	private static readonly Texture2D EmptyBatteryBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

	private static readonly Texture2D DamagedBatteryBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.24f, 0.24f));

	public Gizmo_UAVPowerCell(Comp_FloatingGunRework carrier)
	{
		comp_FloatingGunRework = carrier;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(6f);
		Widgets.DrawWindowBackground(rect);
		Rect rect3 = rect2;
		rect3.height = rect.height / 2f;
		Text.Font = GameFont.Tiny;
		if (!comp_FloatingGunRework.TempDestroyed())
		{
			Widgets.Label(rect3, "CMC_UAVGizmoA".Translate());
		}
		else
		{
			Widgets.Label(rect3, "CMC_UAVGizmoA_Destroyed".Translate());
		}
		Rect rect4 = rect2;
		rect4.yMin = rect2.y + rect2.height / 2f;
		float fillPercent = (float)comp_FloatingGunRework.tickactive / (float)comp_FloatingGunRework.ModifiedBatteryLifeTick;
		if (!comp_FloatingGunRework.TempDestroyed())
		{
			Widgets.FillableBar(rect4, fillPercent, FullBatteryBarTex, EmptyBatteryBarTex, doBorder: false);
		}
		else
		{
			Widgets.FillableBar(rect4, fillPercent, DamagedBatteryBarTex, EmptyBatteryBarTex, doBorder: false);
		}
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect4, (comp_FloatingGunRework.tickactive / 10).ToString());
		Text.Anchor = TextAnchor.UpperLeft;
		TooltipHandler.TipRegion(rect2, "CMC_UAVGizmoB".Translate());
		return new GizmoResult(GizmoState.Clear);
	}

	public override float GetWidth(float maxWidth)
	{
		return 82f;
	}
}
