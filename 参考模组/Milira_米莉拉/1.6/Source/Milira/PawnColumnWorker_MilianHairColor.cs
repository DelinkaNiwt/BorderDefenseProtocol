using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class PawnColumnWorker_MilianHairColor : PawnColumnWorker_Color
{
	public override bool VisibleCurrently => MiliraRaceSettings.MiliraRace_ModSetting_MilianHairColor;

	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		((PawnColumnWorker_Color)this).DoCell(rect, pawn, table);
		CompMilianHairSwitch comp = pawn.TryGetComp<CompMilianHairSwitch>();
		if (Widgets.ButtonInvisible(rect) && comp != null)
		{
			Find.WindowStack.Add(new Dialog_MilianHairStyleConfig(pawn, comp.colorOverride, delegate(Color newColor)
			{
				comp.colorOverride = newColor;
			}, ""));
		}
	}

	protected override Color GetColor(Pawn pawn)
	{
		if (pawn != null)
		{
			if (MiliraRaceSettings.MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride && pawn.TryGetComp<CompMilianHairSwitch>().colorOverride != default(Color))
			{
				return pawn.TryGetComp<CompMilianHairSwitch>().colorOverride;
			}
			return pawn.Faction.AllegianceColor;
		}
		return Color.white;
	}
}
