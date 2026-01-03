using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalDormant : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		CompCanBeDormant compCanBeDormant = pawn.TryGetComp<CompCanBeDormant>();
		if (compCanBeDormant != null && !compCanBeDormant.Awake)
		{
			return true;
		}
		return false;
	}
}
