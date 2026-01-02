using RimWorld;

namespace AlienRace.ExtendedGraphics;

public class ConditionBackstory : Condition
{
	public new const string XmlNameParseKey = "Backstory";

	public BackstoryDef backstory;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.HasBackStory(backstory);
	}
}
