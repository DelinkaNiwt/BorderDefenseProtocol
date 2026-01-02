using RimWorld;

namespace AlienRace.ExtendedGraphics;

public class ConditionAge : Condition
{
	public new const string XmlNameParseKey = "Age";

	public LifeStageDef age;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.CurrentLifeStageDefMatches(age);
	}
}
