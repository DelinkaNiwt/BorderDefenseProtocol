using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public static class MiliraFleckMaker
{
	public static void ThrowPlasmaAirPuffUp(Vector3 loc, Map map, Color color)
	{
		if (loc.ToIntVec3().ShouldSpawnMotesAt(map))
		{
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(Rand.Range(-0.02f, 0.02f), 0f, Rand.Range(-0.02f, 0.02f)), map, FleckDefOf.Milira_AirPuff, 1.5f);
			dataStatic.rotationRate = Rand.RangeInclusive(-240, 240);
			dataStatic.velocityAngle = Rand.Range(-45, 45);
			dataStatic.velocitySpeed = Rand.Range(1.2f, 1.5f);
			dataStatic.instanceColor = color;
			map.flecks.CreateFleck(dataStatic);
		}
	}

	public static void ThrowLineEMP(Vector3 loc, Map map)
	{
		if (loc.ToIntVec3().ShouldSpawnMotesAt(map))
		{
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(Rand.Range(-0.02f, 0.02f), 0f, Rand.Range(-0.02f, 0.02f)), map, FleckDefOf.MicroSparks, 1.5f);
			dataStatic.rotationRate = Rand.RangeInclusive(0, 0);
			dataStatic.velocityAngle = Rand.Range(0, 0);
			dataStatic.velocitySpeed = Rand.Range(1.2f, 1.5f);
			map.flecks.CreateFleck(dataStatic);
		}
	}
}
