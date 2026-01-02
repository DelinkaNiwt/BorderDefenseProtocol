using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionRace : Condition
{
	public new const string XmlNameParseKey = "Race";

	public ThingDef race;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.IsRace(race);
	}
}
