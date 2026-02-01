using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompAbilityEffect_ApplyHediffToFrontline : CompAbilityEffect
{
	public new CompProperties_ApplyHediffToFrontline Props => (CompProperties_ApplyHediffToFrontline)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Map map = parent.pawn.Map;
		Faction faction = parent.pawn.Faction;
		List<IntVec3> enemyPositions = GetEnemyPositions(map, faction);
		List<Pawn> allies = GetValidAllies(map, faction);
		if (allies.Count == 0)
		{
			Log.Message("[ApplyHediff] No valid allies found for " + Props.hediffToApply?.defName);
			return;
		}
		List<Pawn> frontlinePawns = FindFrontlinePawns(allies, enemyPositions);
		foreach (Pawn pawn in frontlinePawns)
		{
			ApplyHediffToPawn(pawn);
		}
	}

	private List<IntVec3> GetEnemyPositions(Map map, Faction faction)
	{
		return (from p in map.mapPawns.AllPawnsSpawned
			where p.Faction != null && p.Faction.HostileTo(faction) && !p.Downed && !p.Dead
			select p.Position).ToList();
	}

	private List<Pawn> GetValidAllies(Map map, Faction faction)
	{
		return map.mapPawns.AllPawnsSpawned.Where((Pawn p) => (Props.affectEnemies || p.Faction == faction) && (Props.includeDowned || !p.Downed) && !p.Dead && !AlreadyHasHediff(p)).ToList();
	}

	private bool AlreadyHasHediff(Pawn pawn)
	{
		if (Props.hediffToApply == null)
		{
			return false;
		}
		return pawn.health.hediffSet.HasHediff(Props.hediffToApply);
	}

	private List<Pawn> FindFrontlinePawns(List<Pawn> allies, List<IntVec3> enemyPositions)
	{
		int targetCount = Mathf.Min(Props.numberOfTargets, allies.Count);
		if (enemyPositions.Count == 0)
		{
			return allies.InRandomOrder().Take(targetCount).ToList();
		}
		List<(Pawn, float)> distances = new List<(Pawn, float)>(allies.Count);
		foreach (Pawn ally in allies)
		{
			float minDistance = float.MaxValue;
			foreach (IntVec3 enemyPos in enemyPositions)
			{
				float dist = ally.Position.DistanceTo(enemyPos);
				if (dist < minDistance)
				{
					minDistance = dist;
				}
			}
			distances.Add((ally, minDistance));
		}
		distances.Sort(((Pawn pawn, float distance) a, (Pawn pawn, float distance) b) => a.distance.CompareTo(b.distance));
		return (from d in distances.Take(targetCount)
			select d.pawn).ToList();
	}

	private void ApplyHediffToPawn(Pawn pawn)
	{
		if (Props.hediffToApply != null && !AlreadyHasHediff(pawn))
		{
			HealthUtility.AdjustSeverity(pawn, Props.hediffToApply, 1f);
			ShowEffect(pawn);
		}
	}

	private void ShowEffect(Pawn pawn)
	{
		FleckMaker.ThrowAirPuffUp(pawn.Position.ToVector3Shifted(), pawn.Map);
	}
}
