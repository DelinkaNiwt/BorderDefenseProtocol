using RimWorld;

namespace AlienRace.ExtendedGraphics;

public class ConditionBodyType : Condition
{
	public new const string XmlNameParseKey = "BodyType";

	public BodyTypeDef bodyType;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.HasBodyType(bodyType);
	}
}
