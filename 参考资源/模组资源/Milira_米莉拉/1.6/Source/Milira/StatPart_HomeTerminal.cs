using System;
using RimWorld;
using Verse;

namespace Milira;

public class StatPart_HomeTerminal : StatPart
{
	public int maxBandWidthWithoutMechlink = 10;

	public override string ExplanationPart(StatRequest req)
	{
		if (req.Thing is Pawn pawn && pawn != null && pawn.health.hediffSet.HasHediff(MiliraDefOf.Milira_MilianHomeTerminal) && !pawn.health.hediffSet.HasHediff(HediffDefOf.MechlinkImplant))
		{
			return "Milira.StatsReport_HomeTerminal".Translate(pawn, maxBandWidthWithoutMechlink.ToString());
		}
		return null;
	}

	public override void TransformValue(StatRequest req, ref float val)
	{
		if (req.Thing is Pawn pawn && pawn != null && pawn.health.hediffSet.HasHediff(MiliraDefOf.Milira_MilianHomeTerminal) && !pawn.health.hediffSet.HasHediff(HediffDefOf.MechlinkImplant))
		{
			val = Math.Min(val, maxBandWidthWithoutMechlink);
		}
	}
}
