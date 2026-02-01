using Verse;
using Verse.AI;

namespace NCLvsTW;

public class ThinkNode_ConditionalInHomeAreaRoofed : ThinkNode_Conditional
{
	public new bool invert = false;

	protected override bool Satisfied(Pawn pawn)
	{
		if (pawn.Map == null)
		{
			return false;
		}
		bool inHomeArea = pawn.Map.areaManager.Home[pawn.Position];
		bool roofed = pawn.Position.Roofed(pawn.Map);
		return invert ? (!(inHomeArea && roofed)) : (inHomeArea && roofed);
	}
}
