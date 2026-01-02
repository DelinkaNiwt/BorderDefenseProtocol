using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobGiver_AICastMechRecover : JobGiver_AICastAbility
{
	private const float MaxDistanceFromCaster = 15f;

	private const float MaxHealthPercent = 0.8f;

	private static readonly SimpleCurve HealthPercentToTargetSelectionWeightCurve = new SimpleCurve
	{
		new CurvePoint(1f, 0f),
		new CurvePoint(0.8f, 0f),
		new CurvePoint(0.5f, 0.5f),
		new CurvePoint(0f, 1f)
	};

	private static List<Pawn> potentialTargets = new List<Pawn>();

	protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
	{
		potentialTargets.Clear();
		IEnumerable<Thing> enumerable = from x in caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster)
			select x.Thing;
		if (enumerable.EnumerableNullOrEmpty())
		{
			return LocalTargetInfo.Invalid;
		}
		foreach (Pawn item in caster.Map.mapPawns.AllPawnsSpawned)
		{
			if (item.health.summaryHealth.SummaryHealthPercent < 0.8f && !item.HostileTo(caster.Faction) && item.Position.InHorDistOf(caster.Position, 15f) && ability.CanApplyOn(new LocalTargetInfo(item)) && item.Map.pathFinder.FindPathNow(caster.Position, item, TraverseParms.For(caster)).Found)
			{
				potentialTargets.Add(item);
			}
		}
		if (((IEnumerable<Thing>)potentialTargets).TryRandomElementByWeight((Func<Thing, float>)delegate
		{
			float num = 0.8f;
			foreach (Pawn potentialTarget in potentialTargets)
			{
				if (potentialTarget.Spawned)
				{
					float summaryHealthPercent = potentialTarget.health.summaryHealth.SummaryHealthPercent;
					if (summaryHealthPercent < num)
					{
						num = summaryHealthPercent;
					}
				}
			}
			return HealthPercentToTargetSelectionWeightCurve.Evaluate(num);
		}, out Thing result))
		{
			return new LocalTargetInfo(result);
		}
		return LocalTargetInfo.Invalid;
	}
}
