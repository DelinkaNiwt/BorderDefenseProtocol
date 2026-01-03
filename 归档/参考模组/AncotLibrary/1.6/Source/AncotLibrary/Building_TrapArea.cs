using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class Building_TrapArea : Building_Trap
{
	public TrapAreaSpringRaduis_Extension Props => def.GetModExtension<TrapAreaSpringRaduis_Extension>();

	protected override void TickInterval(int delta)
	{
		if (!base.Spawned || base.Faction == null)
		{
			return;
		}
		HashSet<IAttackTarget> hashSet = base.Map.attackTargetsCache.TargetsHostileToFaction(base.Faction);
		foreach (IAttackTarget item in hashSet)
		{
			if (!(item is Pawn pawn) || (float)pawn.PositionHeld.DistanceToSquared(base.Position) > Props.springRadius * Props.springRadius || !CheckSpring(pawn))
			{
				continue;
			}
			break;
		}
	}

	private bool CheckSpring(Pawn p)
	{
		if (Rand.Chance(SpringChance(p)))
		{
			Map map = base.Map;
			Spring(p);
			return true;
		}
		return false;
	}

	protected override void SpringSub(Pawn p)
	{
	}
}
