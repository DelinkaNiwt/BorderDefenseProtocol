using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnColumnWorker_Equipment : PawnColumnWorker_Icon
{
	protected override int Padding => 0;

	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		base.DoCell(rect, pawn, table);
		ThingWithComps primary = pawn.equipment.Primary;
		GUI.color = AncotUtility.GetQualityColor(primary);
		Rect rect2 = new Rect(rect.x, rect.y, 7f, 7f);
		GUI.DrawTexture(rect2, (Texture)AncotLibraryIcon.SmallPoint);
		GUI.color = Color.white;
		if (primary != null)
		{
			Widgets.DrawHighlightIfMouseover(rect);
		}
	}

	protected override Texture2D GetIconFor(Pawn pawn)
	{
		return pawn.equipment.Primary?.def.uiIcon;
	}

	protected override Color GetIconColor(Pawn pawn)
	{
		ThingWithComps primary = pawn.equipment.Primary;
		if (primary != null && primary.def.MadeFromStuff)
		{
			return primary.Stuff.stuffProps.color;
		}
		return Color.white;
	}

	protected override string GetIconTip(Pawn pawn)
	{
		string text = pawn.equipment.Primary?.LabelCap;
		if (!text.NullOrEmpty())
		{
			return text;
		}
		return null;
	}
}
