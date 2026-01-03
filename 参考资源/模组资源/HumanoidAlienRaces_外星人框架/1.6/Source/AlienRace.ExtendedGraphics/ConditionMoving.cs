namespace AlienRace.ExtendedGraphics;

public class ConditionMoving : Condition
{
	public new const string XmlNameParseKey = "Moving";

	public bool moving;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return moving == pawn.Moving;
	}
}
