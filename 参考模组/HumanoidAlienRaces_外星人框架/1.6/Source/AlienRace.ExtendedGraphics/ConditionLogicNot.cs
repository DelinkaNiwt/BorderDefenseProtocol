namespace AlienRace.ExtendedGraphics;

public class ConditionLogicNot : ConditionLogicSingle
{
	public new const string XmlNameParseKey = "Not";

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return !condition.Satisfied(pawn, ref data);
	}
}
