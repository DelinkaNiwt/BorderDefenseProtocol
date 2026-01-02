using System;
using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using Verse;

namespace Milira;

public class GenStep_SunLightFuelStation_TechPrintRoom : GenStep_Scatterer
{
	private const int Size = 6;

	public override int SeedPart => 69356159;

	private PrefabDef SunLightFuelStation_TechPrintRoom => MiliraPrefabDefOf.Milian_SunLightFuelStation_TechPrintRoom;

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
		if (!PrefabUtility.CanSpawnPrefab(SunLightFuelStation_TechPrintRoom, map, c, Rot4.Random))
		{
			return false;
		}
		foreach (IntVec3 item in GenRadial.RadialCellsAround(c, 6f, useCenter: true))
		{
			if (!item.InBounds(map) || item.GetEdifice(map) != null)
			{
				return false;
			}
		}
		return true;
	}

	protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
	{
		ThingDef named = DefDatabase<ThingDef>.GetNamed("Techprint_Milira_SunLightFuelGenerator");
		if (named != null && !MiliraDefOf.Milira_SunLightFuelGenerator.IsFinished)
		{
			Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
			Rot4 random = Rot4.Random;
			CellRect var = default(CellRect);
			AncotPrefabUtility.SpawnPrefabRoofed(SunLightFuelStation_TechPrintRoom, RoofDefOf.RoofConstructed, ref var, map, loc, random, faction, (List<Thing>)null, (Func<PrefabThingData, Tuple<ThingDef, ThingDef>>)null, (Action<Thing>)null, false);
			MapGenerator.SetVar("RectOfInterest", var);
		}
	}
}
