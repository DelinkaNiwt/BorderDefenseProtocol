using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class CompAbilityEffect_ApplyHediffInDarkness : CompAbilityEffect
{
	public new CompProperties_AbilityApplyHediffInDarkness Props => (CompProperties_AbilityApplyHediffInDarkness)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (parent.pawn == null || !parent.pawn.Spawned)
		{
			return;
		}
		List<Pawn> enemiesInRange = GetEnemiesInRange(parent.pawn.Position, parent.pawn.Map, Props.maxRange);
		foreach (Pawn enemy in enemiesInRange)
		{
			if (IsInDarkness(enemy))
			{
				enemy.health.AddHediff(Props.hediffToApply);
			}
		}
	}

	private bool IsInDarkness(Pawn pawn)
	{
		if (pawn?.Map == null || !pawn.Spawned)
		{
			return false;
		}
		float glow = pawn.Map.glowGrid.GroundGlowAt(pawn.Position);
		return glow < Props.darknessThreshold;
	}

	private List<Pawn> GetEnemiesInRange(IntVec3 center, Map map, float range)
	{
		List<Pawn> enemies = new List<Pawn>();
		if (map == null)
		{
			return enemies;
		}
		foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
		{
			if (pawn.HostileTo(parent.pawn.Faction) && pawn.Position.DistanceTo(center) <= range)
			{
				enemies.Add(pawn);
			}
		}
		return enemies;
	}
}
