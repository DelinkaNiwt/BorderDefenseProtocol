using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class StatPart_PsycastBodiesConsumed : StatPart
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		if (req.HasThing && req.Thing is Pawn pawn && pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_BodiesConsumed) is Hediff_BodiesConsumed { consumedBodies: >0 } hediff_BodiesConsumed)
		{
			val += hediff_BodiesConsumed.consumedBodies;
		}
	}

	public override string ExplanationPart(StatRequest req)
	{
		if (req.HasThing && req.Thing is Pawn pawn && pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_BodiesConsumed) is Hediff_BodiesConsumed { consumedBodies: >0 } hediff_BodiesConsumed)
		{
			return "VPE.StatsReport_BodiesConsumed".Translate(hediff_BodiesConsumed.consumedBodies);
		}
		return null;
	}
}
