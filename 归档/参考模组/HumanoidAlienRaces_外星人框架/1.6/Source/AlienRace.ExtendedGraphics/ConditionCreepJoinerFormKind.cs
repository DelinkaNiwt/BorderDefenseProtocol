using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionCreepJoinerFormKind : Condition
{
	public new const string XmlNameParseKey = "CreepForm";

	public CreepJoinerFormKindDef form;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.IsCreepJoiner(form);
	}
}
