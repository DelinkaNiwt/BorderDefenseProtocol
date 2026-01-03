using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalCleanArea : ThinkNode_Conditional
{
	public float areaRadius = 0f;

	public bool canRoofed = true;

	protected override bool Satisfied(Pawn pawn)
	{
		IntVec3 position = pawn.Position;
		foreach (IntVec3 item in GenRadial.RadialCellsAround(position, areaRadius, useCenter: false))
		{
			if (!canRoofed && item.Roofed(pawn.Map))
			{
				return false;
			}
			if (!item.IsValid || !item.InBounds(pawn.Map) || !item.Walkable(pawn.Map) || !item.GetEdifice(pawn.Map).DestroyedOrNull())
			{
				return false;
			}
		}
		return true;
	}
}
