using RimWorld;
using Verse;

namespace AncotLibrary;

public class StatPart_ApparelWearerStatFactor : StatPart
{
	public StatDef stat;

	public override void TransformValue(StatRequest req, ref float val)
	{
		PawnOwner(req, out var pawn);
		if (pawn != null)
		{
			val *= pawn.GetStatValue(stat);
		}
	}

	public override string ExplanationPart(StatRequest req)
	{
		PawnOwner(req, out var pawn);
		if (pawn != null)
		{
			float statValue = pawn.GetStatValue(stat);
			return "Ancot.StatsReport_StatEquipped".Translate(stat.LabelCap, statValue.ToStringByStyle(stat.toStringStyle));
		}
		return null;
	}

	public void PawnOwner(StatRequest req, out Pawn pawn)
	{
		pawn = ((Apparel)req.Thing)?.Wearer;
	}
}
