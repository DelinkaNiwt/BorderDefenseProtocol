using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionGene : Condition
{
	public new const string XmlNameParseKey = "Gene";

	public GeneDef gene;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		if (ModsConfig.BiotechActive)
		{
			return pawn.HasGene(gene);
		}
		return true;
	}
}
