using Verse;
using Verse.AI;

namespace NCLWorm;

public class ThinkNode_NCLWormSleep : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		if (pawn is NCL_Pawn_Worm { Sleep: not false })
		{
			return true;
		}
		return false;
	}
}
