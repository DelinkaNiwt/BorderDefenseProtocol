using RimWorld;
using UnityEngine;
using Verse;

namespace TurbojetBackpack;

public class Command_AbilityMagazine : Command_Ability
{
	private CompAbility_Magazine comp;

	public Command_AbilityMagazine(Ability ability, Pawn pawn, CompAbility_Magazine comp)
		: base(ability, pawn)
	{
		this.comp = comp;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
		if (comp != null)
		{
			string text = $"{comp.Charges}/{comp.MaxCharges}";
			GameFont font = Text.Font;
			TextAnchor anchor = Text.Anchor;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperRight;
			Vector2 vector = Text.CalcSize(text);
			float num = 4f;
			float num2 = 2f;
			Rect rect = new Rect(topLeft.x + GetWidth(maxWidth) - vector.x - num, topLeft.y + num2, vector.x, vector.y);
			GUI.color = Color.black;
			Widgets.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text);
			Widgets.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text);
			Widgets.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text);
			Widgets.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text);
			GUI.color = Color.white;
			Widgets.Label(rect, text);
			Text.Font = font;
			Text.Anchor = anchor;
			GUI.color = Color.white;
		}
		return result;
	}
}
