using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalPivotExist : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		return pawn.TryGetComp<CompCommandTerminal>().pivot != null;
	}
}
