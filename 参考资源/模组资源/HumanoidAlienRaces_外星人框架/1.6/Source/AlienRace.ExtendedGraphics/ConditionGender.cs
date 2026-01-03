using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionGender : Condition
{
	public new const string XmlNameParseKey = "Gender";

	public Gender gender;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.GetGender() == gender;
	}
}
