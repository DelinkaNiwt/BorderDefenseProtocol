using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class Gizmo_ChargeBar : Gizmo
{
	public CompWeaponCharge compWeaponCharge;

	private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);

	public Color CustomBarColor
	{
		get
		{
			if (compWeaponCharge.ChargeState == An_ChargeState.Resetting)
			{
				Color barColor = compWeaponCharge.barColor;
				barColor.a = 0.4f;
				return barColor;
			}
			return compWeaponCharge.barColor;
		}
	}

	public Gizmo_ChargeBar()
	{
		Order = -99f;
	}

	public override float GetWidth(float maxWidth)
	{
		return 140f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Texture2D fullBarTex = SolidColorMaterials.NewSolidColorTexture(CustomBarColor);
		Rect overRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Find.WindowStack.ImmediateWindow(1523289473, overRect, WindowLayer.GameUI, delegate
		{
			Rect rect2;
			Rect rect = (rect2 = overRect.AtZero().ContractedBy(6f));
			rect2.height = overRect.height / 2f;
			Text.Font = GameFont.Tiny;
			Widgets.Label(rect2, compWeaponCharge.parent.Label);
			Rect rect3 = rect;
			rect3.yMin = overRect.height / 2f;
			if (compWeaponCharge.ChargeState == An_ChargeState.Resetting)
			{
				float fillPercent = (float)(compWeaponCharge.StartingTicksToReset - compWeaponCharge.ticksToReset) / (float)compWeaponCharge.StartingTicksToReset;
				Widgets.FillableBar(rect3, fillPercent, fullBarTex, EmptyBarTex, doBorder: false);
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(rect3, compWeaponCharge.ticksToReset.TicksToSeconds().ToString("F1") + "Ancot.Second".Translate());
				Text.Anchor = TextAnchor.UpperLeft;
			}
			else
			{
				float fillPercent2 = (float)compWeaponCharge.Charge / (float)compWeaponCharge.maxCharge;
				Widgets.FillableBar(rect3, fillPercent2, fullBarTex, EmptyBarTex, doBorder: false);
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(rect3, compWeaponCharge.Charge.ToString("F0") + " / " + compWeaponCharge.maxCharge.ToString("F0"));
				Text.Anchor = TextAnchor.UpperLeft;
			}
		});
		return new GizmoResult(GizmoState.Clear);
	}
}
