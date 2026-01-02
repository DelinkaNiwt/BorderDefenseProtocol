using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnColumnWorker_DroneWorkMode : PawnColumnWorker_Icon
{
	protected override int Padding => 0;

	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		CompDrone powerCell = pawn.TryGetComp<CompDrone>();
		if (Widgets.ButtonInvisible(rect))
		{
			Find.WindowStack.Add(new FloatMenu(DroneGizmo.GetWorkModeOptions(powerCell).ToList()));
		}
		base.DoCell(rect, pawn, table);
	}

	protected override Texture2D GetIconFor(Pawn pawn)
	{
		return pawn?.TryGetComp<CompDrone>()?.workMode?.uiIcon;
	}

	protected override string GetIconTip(Pawn pawn)
	{
		string text = pawn.TryGetComp<CompDrone>()?.workMode?.description;
		if (!text.NullOrEmpty())
		{
			return text;
		}
		return null;
	}
}
