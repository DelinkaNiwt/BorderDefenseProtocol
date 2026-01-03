namespace AlienRace.ExtendedGraphics;

public class ConditionLogicCollectionAnd : ConditionLogicCollection
{
	public new const string XmlNameParseKey = "And";

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		ResolveData tmpData = data;
		bool logic = conditions.TrueForAll((Condition cd) => cd.Satisfied(pawn, ref tmpData));
		data = tmpData;
		return logic;
	}
}
