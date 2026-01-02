using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_AICastAbility_DistanceBased : JobGiver_AICastAbility
{
	protected SimpleCurve distanceWeightCurve = new SimpleCurve();

	protected float range = 25f;

	protected float minRange = 0f;

	private static List<Thing> potentialTargets = new List<Thing>();

	public virtual bool ValidTarget(Pawn caster, LocalTargetInfo target)
	{
		return true;
	}

	protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
	{
		potentialTargets.Clear();
		IEnumerable<Thing> hostiles = from x in caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster)
			select x.Thing;
		if (hostiles.EnumerableNullOrEmpty())
		{
			return LocalTargetInfo.Invalid;
		}
		foreach (Thing item in hostiles)
		{
			if (item.Fogged() || !ValidTarget(caster, item) || !item.HostileTo(caster.Faction) || !item.Position.InHorDistOf(caster.Position, range) || !(item.Position.DistanceTo(caster.Position) > minRange) || !ability.CanApplyOn(new LocalTargetInfo(item)))
			{
				continue;
			}
			if (item is Pawn)
			{
				Pawn pawn = item as Pawn;
				if (!pawn.Downed)
				{
					potentialTargets.Add(item);
				}
			}
			else
			{
				potentialTargets.Add(item);
			}
		}
		if (potentialTargets.TryRandomElementByWeight(delegate(Thing x)
		{
			float num = 0f;
			foreach (Thing item2 in hostiles)
			{
				if (item2.Spawned)
				{
					float num2 = item2.Position.DistanceTo(x.Position);
					if (num2 > num)
					{
						num = num2;
					}
				}
			}
			return distanceWeightCurve.Evaluate(num);
		}, out var result))
		{
			return new LocalTargetInfo(result);
		}
		return LocalTargetInfo.Invalid;
	}
}
