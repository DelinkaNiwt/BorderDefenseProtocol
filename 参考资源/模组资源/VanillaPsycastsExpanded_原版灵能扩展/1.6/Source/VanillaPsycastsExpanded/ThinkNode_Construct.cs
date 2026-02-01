using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded;

public class ThinkNode_Construct : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		return pawn.def == VPE_DefOf.VPE_Race_RockConstruct;
	}
}
