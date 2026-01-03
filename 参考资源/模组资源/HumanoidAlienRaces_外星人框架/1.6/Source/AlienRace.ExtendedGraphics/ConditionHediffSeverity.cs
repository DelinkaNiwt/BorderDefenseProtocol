using System.Linq;

namespace AlienRace.ExtendedGraphics;

public class ConditionHediffSeverity : Condition
{
	public new const string XmlNameParseKey = "Severity";

	public float severity;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.SeverityOfHediffsOnPart(data.hediff, data.bodyPart, data.bodyPartLabel).Any((float sev) => sev >= severity);
	}
}
