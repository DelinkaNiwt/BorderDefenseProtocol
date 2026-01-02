using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnColumnWorker_AutoFightMech : PawnColumnWorker
{
	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		CompMechAutoFight compMechAutoFight = pawn.TryGetComp<CompMechAutoFight>();
		if (!ModsConfig.BiotechActive || !pawn.IsColonyMechPlayerControlled || !pawn.Spawned || compMechAutoFight == null || !compMechAutoFight.CanAutoFight)
		{
			return;
		}
		AcceptanceReport acceptanceReport = MechanitorUtility.CanDraftMech(pawn);
		if ((bool)acceptanceReport || !acceptanceReport.Reason.NullOrEmpty())
		{
			rect.xMin += (rect.width - 24f) / 2f;
			rect.yMin += (rect.height - 24f) / 2f;
			bool autoFight = compMechAutoFight.AutoFight;
			Widgets.Checkbox(rect.position, ref autoFight, 24f, paintable: def.paintable, disabled: !acceptanceReport);
			if (!acceptanceReport.Reason.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, acceptanceReport.Reason.CapitalizeFirst());
			}
			if (autoFight != compMechAutoFight.AutoFight)
			{
				compMechAutoFight.AutoFight = autoFight;
			}
		}
	}

	public override int GetMinWidth(PawnTable table)
	{
		return Mathf.Max(base.GetMinWidth(table), 44);
	}

	public override int GetMaxWidth(PawnTable table)
	{
		return Mathf.Min(base.GetMaxWidth(table), GetMinWidth(table));
	}

	public override int GetMinCellHeight(Pawn pawn)
	{
		return Mathf.Max(base.GetMinCellHeight(pawn), 24);
	}
}
