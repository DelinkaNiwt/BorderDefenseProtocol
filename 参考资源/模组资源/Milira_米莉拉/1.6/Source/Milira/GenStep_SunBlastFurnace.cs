using System;
using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using Verse;

namespace Milira;

public class GenStep_SunBlastFurnace : GenStep_Scatterer
{
	private const int Size = 7;

	public override int SeedPart => 69356128;

	private PrefabDef SunBlastFurnace_InRoom => MiliraPrefabDefOf.Milira_SunBlastFurnace;

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
		if (!PrefabUtility.CanSpawnPrefab(SunBlastFurnace_InRoom, map, c, Rot4.Random))
		{
			return false;
		}
		foreach (IntVec3 item in CellRect.CenteredOn(c, 7, 7))
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
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		Rot4 random = Rot4.Random;
		CellRect var = default(CellRect);
		AncotPrefabUtility.SpawnPrefabRoofed(SunBlastFurnace_InRoom, RoofDefOf.RoofConstructed, ref var, map, loc, random, faction, (List<Thing>)null, (Func<PrefabThingData, Tuple<ThingDef, ThingDef>>)null, (Action<Thing>)null, false);
		MapGenerator.SetVar("RectOfInterest", var);
	}
}
