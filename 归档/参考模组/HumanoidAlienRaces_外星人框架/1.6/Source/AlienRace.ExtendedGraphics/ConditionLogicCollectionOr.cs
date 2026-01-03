using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionLogicCollectionOr : ConditionLogicCollection
{
	public new const string XmlNameParseKey = "Or";

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		ResolveData tmpData = data;
		bool logic = conditions.Any((Condition cd) => cd.Satisfied(pawn, ref tmpData));
		data = tmpData;
		return logic;
	}
}
