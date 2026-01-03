using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionHediff : Condition
{
	public new const string XmlNameParseKey = "Hediff";

	public HediffDef hediff;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		bool satisfied = pawn.HasHediffOfDefAndPart(hediff, data.bodyPart, data.bodyPartLabel) != null;
		if (satisfied)
		{
			data.hediff = hediff;
		}
		return satisfied;
	}
}
