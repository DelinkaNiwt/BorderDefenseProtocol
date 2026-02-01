using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompExplosiveRefuelable : ThingComp
{
	private CompRefuelable refuelableComp;

	private bool deconstructing;

	public CompProperties_ExplosiveRefuelable Props => (CompProperties_ExplosiveRefuelable)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		refuelableComp = parent.TryGetComp<CompRefuelable>();
	}

	public override void ReceiveCompSignal(string signal)
	{
		if (signal == "StartDeconstruct")
		{
			deconstructing = true;
		}
		base.ReceiveCompSignal(signal);
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		if (!deconstructing && mode != DestroyMode.Deconstruct && (!Props.requiresFuelForExplosion || (refuelableComp != null && !(refuelableComp.Fuel <= 0f))))
		{
			TriggerExplosion(previousMap);
		}
	}

	private void TriggerExplosion(Map map)
	{
		if (map != null)
		{
			float fuelAmount = refuelableComp?.Fuel ?? 0f;
			float maxFuel = refuelableComp?.Props.fuelCapacity ?? 1f;
			float fuelRatio = fuelAmount / maxFuel;
			float explosionRadius = Mathf.Lerp(Props.minExplosionRadius, Props.maxExplosionRadius, fuelRatio);
			float clearRadius = Mathf.Lerp(Props.minClearRadius, Props.maxClearRadius, fuelRatio);
			int damage = Mathf.RoundToInt(fuelAmount * Props.explosionDamageFactor);
			ClearDropsInRadius(map, parent.Position, clearRadius);
			CreateCenterFlecks(map, fuelRatio);
			CreateCenterSmoke(map, fuelRatio, explosionRadius);
			GenExplosion.DoExplosion(parent.Position, map, explosionRadius, Props.damageDef, parent, Mathf.Max(10, damage), -1f, null, null, null, null, ThingDefOf.Filth_Fuel, 0.75f, Mathf.RoundToInt(fuelAmount * 0.1f), null, null, 255, applyDamageToExplosionCellsNeighbors: true, null, 0f, 1, 0.8f);
			CreateDelayedEffects(map, fuelRatio, explosionRadius);
		}
	}

	private void CreateCenterFlecks(Map map, float fuelRatio)
	{
		for (int i = 0; i < 10; i++)
		{
			Vector3 spawnPos = parent.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(0.3f);
			float fleckSize = Mathf.Lerp(10f, 50f, fuelRatio);
			fleckSize *= Rand.Range(0.8f, 1.2f);
			FleckCreationData data = FleckMaker.GetDataStatic(spawnPos, map, FleckDefOf.ExplosionFlash, fleckSize);
			map.flecks.CreateFleck(data);
		}
	}

	private void CreateCenterSmoke(Map map, float fuelRatio, float explosionRadius)
	{
		int centerSmokeCount = Mathf.RoundToInt(5f + fuelRatio * 5f);
		float centerSmokeSize = 2f + fuelRatio * 3f;
		for (int i = 0; i < centerSmokeCount; i++)
		{
			Vector3 spawnPos = parent.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(0.5f);
			float smokeSize = centerSmokeSize * Rand.Range(0.8f, 1.2f);
			FleckMaker.ThrowSmoke(spawnPos, map, smokeSize);
		}
	}

	private void CreateDelayedEffects(Map map, float fuelRatio, float explosionRadius)
	{
		DelayedEffectManager delayedEffects = map.GetComponent<DelayedEffectManager>();
		if (delayedEffects == null)
		{
			delayedEffects = new DelayedEffectManager(map);
			map.components.Add(delayedEffects);
		}
		delayedEffects.AddDelayedAction(new DelayedEffect
		{
			Action = delegate
			{
				CreateSurroundingFlecks(map, fuelRatio, explosionRadius);
			},
			DelayTicks = 10,
			StartTick = Find.TickManager.TicksGame
		});
		delayedEffects.AddDelayedAction(new DelayedEffect
		{
			Action = delegate
			{
				CreateSurroundingSmoke(map, fuelRatio, explosionRadius);
			},
			DelayTicks = 10,
			StartTick = Find.TickManager.TicksGame
		});
	}

	private void CreateSurroundingFlecks(Map map, float fuelRatio, float explosionRadius)
	{
		int extraFleckCount = Mathf.RoundToInt(fuelRatio * 100f);
		for (int i = 0; i < extraFleckCount; i++)
		{
			IntVec3 cell = parent.Position + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(explosionRadius))];
			if (cell.InBounds(map))
			{
				Vector3 spawnPos = cell.ToVector3Shifted() + Gen.RandomHorizontalVector(0.5f);
				float fleckSize = Mathf.Lerp(1f, 30f, fuelRatio) * Rand.Range(0.7f, 1.3f);
				FleckCreationData data = FleckMaker.GetDataStatic(spawnPos, map, FleckDefOf.ExplosionFlash, fleckSize);
				map.flecks.CreateFleck(data);
			}
		}
	}

	private void CreateSurroundingSmoke(Map map, float fuelRatio, float explosionRadius)
	{
		int smokeCount = Mathf.RoundToInt(10f + explosionRadius * 5f);
		float baseSmokeSize = 0.7f + explosionRadius / Props.maxExplosionRadius * 3f;
		for (int i = 0; i < smokeCount; i++)
		{
			IntVec3 randomCell = parent.Position + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(explosionRadius))];
			if (!(randomCell.DistanceTo(parent.Position) < 3f) && randomCell.InBounds(map))
			{
				float smokeSize = baseSmokeSize * Rand.Range(0.7f, 13f);
				Vector3 spawnPos = randomCell.ToVector3Shifted() + Gen.RandomHorizontalVector(0.5f);
				FleckMaker.ThrowSmoke(spawnPos, map, smokeSize);
			}
		}
	}

	private void ClearDropsInRadius(Map map, IntVec3 center, float radius)
	{
		if (map == null || radius <= 0f)
		{
			return;
		}
		int numCellsInRadius = GenRadial.NumCellsInRadius(radius);
		for (int i = 0; i < numCellsInRadius; i++)
		{
			IntVec3 cell = center + GenRadial.RadialPattern[i];
			if (!cell.InBounds(map))
			{
				continue;
			}
			List<Thing> thingsAtCell = map.thingGrid.ThingsListAt(cell);
			for (int j = thingsAtCell.Count - 1; j >= 0; j--)
			{
				Thing thing = thingsAtCell[j];
				if (thing != parent)
				{
					thing.Destroy();
				}
			}
		}
	}
}
