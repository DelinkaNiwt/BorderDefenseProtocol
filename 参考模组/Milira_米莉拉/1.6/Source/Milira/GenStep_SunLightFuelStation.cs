using AncotLibrary;
using RimWorld;
using Verse;

namespace Milira;

public class GenStep_SunLightFuelStation : GenStep_Scatterer
{
	private const int Size = 7;

	public override int SeedPart => 69356159;

	private PrefabDef SunLightFuelStation => MiliraPrefabDefOf.Milira_SunLightFuelStation;

	protected override bool CanScatterAt(IntVec3 c, Map map)
	{
		if (!base.CanScatterAt(c, map))
		{
			return false;
		}
		if (!c.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy))
		{
			return false;
		}
		if (!map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors)))
		{
			return false;
		}
		if (!PrefabUtility.CanSpawnPrefab(SunLightFuelStation, map, c, Rot4.Random))
		{
			return false;
		}
		foreach (IntVec3 item in GenRadial.RadialCellsAround(c, 10f, useCenter: true))
		{
			if (!item.InBounds(map) || item.GetEdifice(map) != null || item.Roofed(map))
			{
				return false;
			}
		}
		return true;
	}

	protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		Rot4 random = Rot4.Random;
		CellRect prefabRect = AncotPrefabUtility.GetPrefabRect(SunLightFuelStation, loc, random, map);
		MapGenerator.SetVar("RectOfInterest", prefabRect);
		PrefabUtility.SpawnPrefab(SunLightFuelStation, map, loc, random, faction);
	}
}
