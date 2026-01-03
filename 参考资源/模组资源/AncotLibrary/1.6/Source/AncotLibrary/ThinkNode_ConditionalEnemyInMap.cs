using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalEnemyInMap : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		foreach (Pawn item in pawn.Map.mapPawns.AllPawnsSpawned)
		{
			if (item.HostileTo(pawn))
			{
				return true;
			}
		}
		return false;
	}
}
