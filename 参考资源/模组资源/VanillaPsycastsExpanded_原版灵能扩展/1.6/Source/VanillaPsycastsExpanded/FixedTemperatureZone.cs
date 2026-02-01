using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

public class FixedTemperatureZone : IExposable
{
	public IntVec3 center;

	public float radius;

	public int expiresIn;

	public float fixedTemperature;

	public FleckDef fleckToSpawn;

	public float spawnRate;

	public void DoEffects(Map map)
	{
		foreach (IntVec3 item in GenRadial.RadialCellsAround(center, radius, useCenter: true))
		{
			if (Rand.Value < spawnRate)
			{
				ThrowFleck(item, map, 2.3f);
				if (fixedTemperature < 0f)
				{
					map.snowGrid.AddDepth(item, 0.1f);
				}
			}
		}
	}

	public void ThrowFleck(IntVec3 c, Map map, float size)
	{
		Vector3 vector = c.ToVector3Shifted();
		if (vector.ShouldSpawnMotesAt(map))
		{
			vector += size * new Vector3(Rand.Value - 0.5f, 0f, Rand.Value - 0.5f);
			if (vector.InBounds(map))
			{
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, map, fleckToSpawn, Rand.Range(4f, 6f) * size);
				dataStatic.rotationRate = Rand.Range(-3f, 3f);
				dataStatic.velocityAngle = Rand.Range(0, 360);
				dataStatic.velocitySpeed = 0.12f;
				map.flecks.CreateFleck(dataStatic);
			}
		}
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref center, "center");
		Scribe_Values.Look(ref radius, "radius", 0f);
		Scribe_Values.Look(ref expiresIn, "expiresIn", 0);
		Scribe_Values.Look(ref fixedTemperature, "fixedTemperature", 0f);
		Scribe_Values.Look(ref spawnRate, "spawnRate", 0f);
		Scribe_Defs.Look(ref fleckToSpawn, "fleckSpawn");
	}
}
