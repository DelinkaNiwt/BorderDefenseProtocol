namespace AlienRace.ExtendedGraphics;

public class ConditionStatue : Condition
{
	public new const string XmlNameParseKey = "Statue";

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.IsStatue;
	}
}
