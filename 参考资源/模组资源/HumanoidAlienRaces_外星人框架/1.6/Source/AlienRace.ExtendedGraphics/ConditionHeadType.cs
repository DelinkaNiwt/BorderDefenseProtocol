using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionHeadType : Condition
{
	public new const string XmlNameParseKey = "HeadType";

	public HeadTypeDef headType;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.HasHeadTypeNamed(headType);
	}
}
