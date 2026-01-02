using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnColumnWorker_DraftDrone : PawnColumnWorker
{
	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		if (!ModsConfig.BiotechActive || !pawn.IsColonyMech || !pawn.Spawned)
		{
			return;
		}
		AcceptanceReport acceptanceReport = CanDraftDrone(pawn);
		if ((bool)acceptanceReport || !acceptanceReport.Reason.NullOrEmpty())
		{
			rect.xMin += (rect.width - 24f) / 2f;
			rect.yMin += (rect.height - 24f) / 2f;
			bool drafted = pawn.Drafted;
			Widgets.Checkbox(rect.position, ref drafted, 24f, paintable: def.paintable, disabled: !acceptanceReport);
			if (!acceptanceReport.Reason.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, acceptanceReport.Reason.CapitalizeFirst());
			}
			if (drafted != pawn.Drafted)
			{
				pawn.drafter.Drafted = drafted;
			}
		}
	}

	public static AcceptanceReport CanDraftDrone(Pawn mech)
	{
		CompDrone compDrone = mech.TryGetComp<CompDrone>();
		if (mech.IsColonyMech && compDrone != null)
		{
			if (!compDrone.Draftable)
			{
				return "Ancot.DroneDraft_NotAvailable".Translate(mech.Named("PAWN"));
			}
			return true;
		}
		return false;
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
