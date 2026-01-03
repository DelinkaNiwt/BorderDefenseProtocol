using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobGiver_AICastUnitAssistance : JobGiver_AICastAbilityCategory
{
	private const float MaxDistanceFromCaster = 15f;

	private const float MaxHealthPercent = 0.8f;

	private static readonly SimpleCurve HealthPercentToTargetSelectionWeightCurve = new SimpleCurve
	{
		new CurvePoint(1f, 0.1f),
		new CurvePoint(0.5f, 0.5f),
		new CurvePoint(0f, 1f)
	};

	private static List<Thing> potentialTargets = new List<Thing>();

	protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
	{
		potentialTargets.Clear();
		IEnumerable<Thing> enumerable = from x in caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster)
			select x.Thing;
		if (enumerable.EnumerableNullOrEmpty())
		{
			return LocalTargetInfo.Invalid;
		}
		IEnumerable<Pawn> enumerable2 = caster.Map.mapPawns.AllPawnsSpawned.Where((Pawn p) => caster.CanReserve(p) && p.RaceProps.IsMechanoid && p.health.summaryHealth.SummaryHealthPercent < 0.8f && p.Faction == caster.Faction && p.Position.InHorDistOf(caster.Position, 26.9f) && ability.CanApplyOn(new LocalTargetInfo(p)));
		foreach (Pawn item in enumerable2)
		{
			potentialTargets.Add(item);
		}
		foreach (Building item2 in caster.Map.listerBuildings.allBuildingsNonColonist)
		{
			if (caster.CanReserve(item2) && item2.Faction == caster.Faction && item2.HitPoints < item2.MaxHitPoints && item2.def.building.combatPower > 0f)
			{
				potentialTargets.Add(item2);
			}
		}
		if (potentialTargets.TryRandomElementByWeight(delegate
		{
			float num = 1f;
			foreach (Thing potentialTarget in potentialTargets)
			{
				if (potentialTarget.Spawned)
				{
					if (potentialTarget is Pawn)
					{
						Pawn pawn = potentialTarget as Pawn;
						float num2 = pawn.health.summaryHealth.SummaryHealthPercent;
						if (potentialTarget.def.defName == "Milian_Mechanoid_BishopII")
						{
							num2 += 0.2f;
						}
						if (num2 < num)
						{
							num = num2;
						}
					}
					else
					{
						float num3 = (float)(potentialTarget.HitPoints / potentialTarget.MaxHitPoints) * 0.8f;
						if (num3 < num)
						{
							num = num3;
						}
					}
				}
			}
			return HealthPercentToTargetSelectionWeightCurve.Evaluate(num);
		}, out var result))
		{
			return new LocalTargetInfo(result);
		}
		return LocalTargetInfo.Invalid;
	}
}
