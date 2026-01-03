using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalThreatDistance : ThinkNode_Conditional
{
	public float? threatDistance;

	protected override bool Satisfied(Pawn pawn)
	{
		Map map = pawn.Map;
		if (!pawn.Downed && !pawn.Dead && pawn.equipment != null)
		{
			float num = threatDistance ?? pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range;
			foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
			{
				if (item.HostileTo(pawn))
				{
					float num2 = item.Position.DistanceTo(pawn.Position);
					if (num2 < num)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		ThinkNode_ConditionalThreatDistance thinkNode_ConditionalThreatDistance = (ThinkNode_ConditionalThreatDistance)base.DeepCopy(resolve);
		thinkNode_ConditionalThreatDistance.threatDistance = threatDistance;
		return thinkNode_ConditionalThreatDistance;
	}
}
