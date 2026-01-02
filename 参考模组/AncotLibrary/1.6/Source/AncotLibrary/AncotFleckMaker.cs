using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public static class AncotFleckMaker
{
	public static void CustomFleckThrow(Map map, FleckDef fleckDef, Vector3 loc, Color color, Vector3 offset = default(Vector3), float scale = 1f, float rotationRate = 0f, float velocityAngle = 0f, float velocitySpeed = 0f, float rotation = 0f)
	{
		if (loc.ToIntVec3().ShouldSpawnMotesAt(map))
		{
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + offset, map, fleckDef, scale);
			dataStatic.rotationRate = rotationRate;
			dataStatic.velocityAngle = velocityAngle;
			dataStatic.velocitySpeed = velocitySpeed;
			dataStatic.instanceColor = color;
			dataStatic.scale = scale;
			dataStatic.rotation = rotation;
			map.flecks.CreateFleck(dataStatic);
		}
	}

	public static void ThrowTrailFleckUp(Vector3 loc, Map map, Color color, FleckDef fleckDef)
	{
		if (loc.ToIntVec3().ShouldSpawnMotesAt(map))
		{
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(Rand.Range(-0.02f, 0.02f), 0f, Rand.Range(-0.02f, 0.02f)), map, fleckDef, 1.5f);
			dataStatic.rotationRate = Rand.RangeInclusive(-240, 240);
			dataStatic.velocityAngle = Rand.Range(-45, 45);
			dataStatic.velocitySpeed = Rand.Range(1.2f, 1.5f);
			dataStatic.instanceColor = color;
			map.flecks.CreateFleck(dataStatic);
		}
	}
}
