using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class Gizmo_PhysicalShieldBar : Gizmo
{
	public CompPhysicalShield compPhysicalShield;

	private Color customBarColor = new Color(0.68f, 0.68f, 0.68f);

	private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);

	private static readonly Texture2D TargetLevelArrow = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated");

	private const float ArrowScale = 0.5f;

	public Color CustomBarColor
	{
		get
		{
			if (compPhysicalShield.ShieldState == An_ShieldState.Resetting)
			{
				Color shieldBarColor = compPhysicalShield.shieldBarColor;
				shieldBarColor.a = 0.4f;
				return shieldBarColor;
			}
			return compPhysicalShield.shieldBarColor;
		}
	}

	public Gizmo_PhysicalShieldBar()
	{
		Order = -99f;
	}

	public override float GetWidth(float maxWidth)
	{
		return 140f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		customBarColor = compPhysicalShield.shieldBarColor;
		Texture2D fullBarTex = SolidColorMaterials.NewSolidColorTexture(customBarColor);
		Rect overRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Find.WindowStack.ImmediateWindow(1523115973, overRect, WindowLayer.GameUI, delegate
		{
			Rect rect2;
			Rect rect = (rect2 = overRect.AtZero().ContractedBy(6f));
			rect2.height = overRect.height / 2f;
			Text.Font = GameFont.Tiny;
			Widgets.Label(rect2, compPhysicalShield.BarGizmoLabel);
			Rect rect3 = rect;
			rect3.yMin = overRect.height / 2f;
			if (compPhysicalShield.ShieldState == An_ShieldState.Resetting)
			{
				float fillPercent = (float)(compPhysicalShield.StartingTicksToReset - compPhysicalShield.ticksToReset) / (float)compPhysicalShield.StartingTicksToReset;
				Widgets.FillableBar(rect3, fillPercent, fullBarTex, EmptyBarTex, doBorder: false);
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(rect3, compPhysicalShield.ticksToReset.TicksToSeconds().ToString("F1") + "Ancot.Second".Translate());
				Text.Anchor = TextAnchor.UpperLeft;
			}
			else
			{
				float fillPercent2 = compPhysicalShield.Stamina / compPhysicalShield.MaxStamina;
				Widgets.FillableBar(rect3, fillPercent2, fullBarTex, EmptyBarTex, doBorder: false);
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(rect3, compPhysicalShield.Stamina.ToString("F0") + " / " + compPhysicalShield.MaxStamina.ToString("F0"));
				Text.Anchor = TextAnchor.UpperLeft;
			}
		});
		return new GizmoResult(GizmoState.Clear);
	}
}
