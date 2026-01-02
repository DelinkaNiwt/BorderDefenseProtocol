namespace AlienRace.ExtendedGraphics;

public class ConditionDrafted : Condition
{
	public new const string XmlNameParseKey = "Drafted";

	public bool drafted = true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return drafted == pawn.Drafted;
	}
}
