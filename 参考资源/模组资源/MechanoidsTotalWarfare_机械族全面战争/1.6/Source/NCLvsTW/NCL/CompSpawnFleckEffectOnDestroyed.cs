using UnityEngine;
using Verse;

namespace NCL;

public class CompSpawnFleckEffectOnDestroyed : ThingComp
{
	public CompProperties_SpawnFleckEffectOnDestroyed Props => (CompProperties_SpawnFleckEffectOnDestroyed)props;

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		if (mode == DestroyMode.Vanish && previousMap != null && (!(parent is Projectile projectile) || projectile.usedTarget.IsValid))
		{
			SpawnEffects(previousMap);
		}
	}

	private void SpawnEffects(Map map)
	{
		MapEffectDurationTracker tracker = map.GetComponent<MapEffectDurationTracker>();
		if (tracker == null)
		{
			tracker = new MapEffectDurationTracker(map);
			map.components.Add(tracker);
		}
		if (Props.flecks != null)
		{
			foreach (FleckData fleckData in Props.flecks)
			{
				for (int i = 0; i < fleckData.count; i++)
				{
					FleckCreationData data = new FleckCreationData
					{
						def = fleckData.fleckDef,
						spawnPosition = parent.Position.ToVector3Shifted() + GetRandomOffset(fleckData.maxOffset),
						scale = fleckData.scale * Props.globalScaleFactor,
						rotationRate = fleckData.rotationRate,
						velocityAngle = fleckData.velocityAngle,
						velocitySpeed = fleckData.velocitySpeed,
						solidTimeOverride = fleckData.solidTime,
						ageTicksOverride = fleckData.durationTicks
					};
					map.flecks.CreateFleck(data);
				}
			}
		}
		if (Props.effects == null)
		{
			return;
		}
		foreach (EffectData effectData in Props.effects)
		{
			for (int j = 0; j < effectData.count; j++)
			{
				IntVec3 spawnPos = (parent.Position.ToVector3Shifted() + GetRandomOffset(effectData.maxOffset)).ToIntVec3();
				Effecter effecter = effectData.effectDef.Spawn();
				effecter.Trigger(new TargetInfo(spawnPos, map), new TargetInfo(spawnPos, map));
				if (effectData.durationTicks > 0)
				{
					tracker.AddEffecterForDuration(effecter, effectData.durationTicks);
				}
				else
				{
					effecter.Cleanup();
				}
			}
		}
	}

	private Vector3 GetRandomOffset(float maxOffset)
	{
		return (maxOffset <= 0f) ? Vector3.zero : new Vector3(Rand.Range(0f - maxOffset, maxOffset), 0f, Rand.Range(0f - maxOffset, maxOffset));
	}
}
