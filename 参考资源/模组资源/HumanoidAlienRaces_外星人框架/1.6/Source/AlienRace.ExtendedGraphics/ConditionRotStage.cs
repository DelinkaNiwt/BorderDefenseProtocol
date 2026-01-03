using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionRotStage : Condition
{
	public new const string XmlNameParseKey = "RotStage";

	public RotDrawMode allowedStages = RotDrawMode.Fresh;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return allowedStages.HasFlag(pawn.GetRotDrawMode());
	}
}
