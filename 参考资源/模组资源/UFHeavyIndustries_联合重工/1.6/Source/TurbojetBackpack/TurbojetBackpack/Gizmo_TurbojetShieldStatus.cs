using RimWorld;
using UnityEngine;
using Verse;

namespace TurbojetBackpack;

[StaticConstructorOnStartup]
public class Gizmo_TurbojetShieldStatus : Gizmo
{
	public CompTurbojetShield shield;

	private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));

	private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

	public override float GetWidth(float maxWidth)
	{
		return 200f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(6f);
		Widgets.DrawWindowBackground(rect);
		Rect rect3 = rect2;
		rect3.height = rect.height / 2f;
		string label = "Turbojet_Shield_Status".Translate(shield.Energy.ToString("F0"), shield.MaxEnergy.ToString("F0"));
		string text = "";
		Texture2D fullShieldBarTex = FullShieldBarTex;
		if (shield.State == ShieldState.Resetting)
		{
			text = "Turbojet_Shield_Recharging".Translate();
		}
		Widgets.FillableBar(rect3, shield.Energy / shield.MaxEnergy, fullShieldBarTex, EmptyShieldBarTex, doBorder: false);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect3, label);
		Rect rect4 = rect2;
		rect4.y += rect3.height;
		rect4.height = rect3.height;
		if (!text.NullOrEmpty())
		{
			Widgets.Label(rect4, text);
		}
		Text.Anchor = TextAnchor.UpperLeft;
		return new GizmoResult(GizmoState.Clear);
	}
}
