namespace AlienRace.ExtendedGraphics;

public class ConditionTrait : Condition
{
	public new const string XmlNameParseKey = "Trait";

	public string trait;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.HasTraitWithIdentifier(trait);
	}
}
