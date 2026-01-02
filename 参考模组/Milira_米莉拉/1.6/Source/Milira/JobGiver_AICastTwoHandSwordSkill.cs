using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobGiver_AICastTwoHandSwordSkill : JobGiver_AICastAbility
{
	private const float MaxDistanceFromCaster = 40f;

	private const float MaxSquareDistanceFromTarget = 1600f;

	private static readonly SimpleCurve DistanceSquaredToTargetSelectionWeightCurve = new SimpleCurve
	{
		new CurvePoint(100f, 0f),
		new CurvePoint(900f, 0.1f),
		new CurvePoint(1600f, 1f)
	};

	private static List<Thing> potentialTargets = new List<Thing>();

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
			if (item.Fogged() || !item.HostileTo(caster.Faction) || !item.Position.InHorDistOf(caster.Position, 25f) || !ability.CanApplyOn(new LocalTargetInfo(item)))
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
			float num = 100f;
			foreach (Thing item2 in hostiles)
			{
				if (item2.Spawned)
				{
					float num2 = item2.Position.DistanceToSquared(x.Position);
					if (num2 > num)
					{
						num = num2;
					}
				}
			}
			return DistanceSquaredToTargetSelectionWeightCurve.Evaluate(num);
		}, out var result))
		{
			return new LocalTargetInfo(result);
		}
		return LocalTargetInfo.Invalid;
	}
}
