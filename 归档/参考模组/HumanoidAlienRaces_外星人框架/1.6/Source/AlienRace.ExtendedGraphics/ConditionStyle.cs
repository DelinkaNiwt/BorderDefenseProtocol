using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionStyle : Condition
{
	public new const string XmlNameParseKey = "Style";

	public StyleCategoryDef style;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.HasStyle(style);
	}
}
