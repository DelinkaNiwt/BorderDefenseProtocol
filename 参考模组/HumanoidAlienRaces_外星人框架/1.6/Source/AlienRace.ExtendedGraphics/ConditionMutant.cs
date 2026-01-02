using RimWorld;

namespace AlienRace.ExtendedGraphics;

public class ConditionMutant : Condition
{
	public new const string XmlNameParseKey = "Mutant";

	public MutantDef mutant;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.IsMutant(mutant);
	}
}
