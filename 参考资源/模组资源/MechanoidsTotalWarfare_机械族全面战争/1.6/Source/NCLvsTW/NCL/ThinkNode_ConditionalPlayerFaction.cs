using Verse;
using Verse.AI;

namespace NCL;

public class ThinkNode_ConditionalPlayerFaction : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		return pawn.Faction?.IsPlayer ?? false;
	}
}
