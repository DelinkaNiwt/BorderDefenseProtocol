using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalNonPlayerMechOrAutoFight : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		CompMechAutoFight compMechAutoFight = pawn.TryGetComp<CompMechAutoFight>();
		return !pawn.IsColonyMech || compMechAutoFight.AutoFight;
	}
}
