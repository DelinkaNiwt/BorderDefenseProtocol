using RimWorld;
using UnityEngine;
using Verse;

namespace NCLWorm;

[StaticConstructorOnStartup]
internal class Gizmo_MechNCLWorm_ShieldStatus : Gizmo
{
	public CompShieldNCLWorm shield;

	private static readonly Texture2D RedShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.6f, 0.07f));

	private static readonly Texture2D BlueShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.39f, 0.58f, 0.93f));

	private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

	public Gizmo_MechNCLWorm_ShieldStatus()
	{
		Order = -200f;
	}

	public override float GetWidth(float maxWidth)
	{
		return 240f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Widgets.DrawWindowBackground(rect);
		Widgets.DrawTextureFitted(rect, NCLWormTexCommand.ShieldGzimoBase, 1f);
		Rect rect2 = rect.ContractedBy(6f);
		Rect rect3 = new Rect(rect2.x, rect2.y, 30f, rect2.height / 2f);
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect3, "S.H.".Translate().Resolve());
		Rect rect4 = new Rect(rect2.x + 30f, rect2.y, rect2.width - 30f, rect2.height / 2f);
		Rect rect5 = rect4.ContractedBy(8f, 6f);
		float fillPercent = Mathf.Min(1f, shield.Power / shield.MaxPower);
		Widgets.FillableBar(rect5, fillPercent, BlueShieldBarTex, EmptyShieldBarTex, doBorder: false);
		bool flag = true;
		DrawBarDivision(rect5, 0.1f, fillPercent);
		DrawBarDivision(rect5, 0.4f, fillPercent);
		DrawBarDivision(rect5, 0.7f, fillPercent);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		string label = shield.Power.ToString("F0") + " / " + shield.MaxPower.ToString("F0");
		if (shield.shieldState == ShieldState.Resetting)
		{
			label = "-" + (shield.ticksToReset / 60).ToString("F1");
		}
		Widgets.Label(rect5, label);
		Rect rect6 = rect3;
		rect6.y += rect3.height;
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect6, "H.P.".Translate().Resolve());
		Rect rect7 = rect4;
		rect7.y += rect4.height;
		Rect rect8 = rect7.ContractedBy(8f, 6f);
		float fillPercent2 = Mathf.Min(1f, GetInyfloat((Pawn)shield.parent) / (((Pawn)shield.parent).HealthScale * 175f));
		Widgets.FillableBar(rect8, fillPercent2, RedShieldBarTex, EmptyShieldBarTex, doBorder: false);
		bool flag2 = true;
		DrawBarDivision(rect8, 0.1f, fillPercent);
		DrawBarDivision(rect8, 0.4f, fillPercent);
		DrawBarDivision(rect8, 0.7f, fillPercent);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		string label2 = GetInyfloat((Pawn)shield.parent).ToString("F0") + "/" + (((Pawn)shield.parent).HealthScale * 175f).ToString("F0");
		Widgets.Label(rect8, label2);
		Text.Anchor = TextAnchor.UpperLeft;
		return new GizmoResult(GizmoState.Clear);
	}

	private float GetInyfloat(Pawn pawn)
	{
		float num = pawn.HealthScale * 175f;
		foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
		{
			if (hediff is Hediff_Injury)
			{
				num -= hediff.Severity;
			}
		}
		return num;
	}

	private void DrawBarDivision(Rect barRect, float threshPct, float fillPercent)
	{
		float num = 5f;
		Rect rect = new Rect(barRect.x + barRect.width * threshPct - (num - 1f), barRect.y, num, barRect.height);
		if (threshPct < fillPercent)
		{
			GUI.color = new Color(0f, 0f, 0f, 0.9f);
		}
		else
		{
			GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		}
		Rect rect2 = rect;
		rect2.yMax = rect2.yMin + 4f;
		GUI.DrawTextureWithTexCoords(rect2, (Texture)NCLWormTexCommand.NeedUnitDividerTex, new Rect(0f, 0.5f, 1f, 0.5f));
		Rect rect3 = rect;
		rect3.yMin = rect3.yMax - 4f;
		GUI.DrawTextureWithTexCoords(rect3, (Texture)NCLWormTexCommand.NeedUnitDividerTex, new Rect(0f, 0f, 1f, 0.5f));
		Rect rect4 = rect;
		rect4.yMin = rect2.yMax;
		rect4.yMax = rect3.yMin;
		if (rect4.height > 0f)
		{
			GUI.DrawTextureWithTexCoords(rect4, (Texture)NCLWormTexCommand.NeedUnitDividerTex, new Rect(0f, 0.4f, 1f, 0.2f));
		}
		GUI.color = Color.white;
	}
}
