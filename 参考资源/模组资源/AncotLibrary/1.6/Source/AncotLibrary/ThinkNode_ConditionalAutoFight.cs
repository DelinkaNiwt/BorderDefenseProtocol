using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalAutoFight : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		CompMechAutoFight compMechAutoFight = pawn.TryGetComp<CompMechAutoFight>();
		return compMechAutoFight.AutoFight;
	}
}
