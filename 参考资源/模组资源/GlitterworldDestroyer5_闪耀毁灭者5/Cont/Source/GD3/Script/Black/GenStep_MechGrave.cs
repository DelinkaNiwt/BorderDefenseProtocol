using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
    public class GenStep_MechGrave : GenStep_Scatterer
    {
		private const int DebrisRadius = 4;

		private const int AsphaltSize = 30;

		private const int ClearRadius = 5;

		private static readonly IntRange BloodFilthRange = new IntRange(3, 4);

		public override int SeedPart => 345673948;

		protected override bool CanScatterAt(IntVec3 loc, Map map)
		{
			if (!base.CanScatterAt(loc, map))
			{
				return false;
			}
			CellRect cellRect = CellRect.CenteredOn(loc, 5);
			int newZ = cellRect.minZ - 1;
			for (int i = cellRect.minX; i <= cellRect.maxX; i++)
			{
				IntVec3 c = new IntVec3(i, 0, newZ);
				if (!c.InBounds(map) || !c.Walkable(map))
				{
					return false;
				}
			}
			return true;
		}

		protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
		{
			GenerateGrave(loc, map);
		}

		public static void GenerateGrave(IntVec3 loc, Map map)
		{
			Building_Sarcophagus thing = (Building_Sarcophagus)ThingMaker.MakeThing(ThingDefOf.Sarcophagus, ThingDefOf.Uranium);
			Thing corpse = ThingMaker.MakeThing(GDDefOf.GD_MechCorpse);
			GenSpawn.Spawn(thing, loc, map);
			thing.TryAcceptThing(corpse);
			foreach (IntVec3 item in GridShapeMaker.IrregularLump(loc, map, 8))
			{
				map.terrainGrid.SetTerrain(item, TerrainDefOf.Ice);
			}
			foreach (IntVec3 item in GridShapeMaker.IrregularLump(loc + new IntVec3 (0, 0, 1), map, 8))
			{
				map.terrainGrid.SetTerrain(item, TerrainDefOf.Ice);
			}
			IEnumerable<Thing> enumerable = from x in map.listerThings.AllThings
											where x is Filth && x.Position.DistanceTo(loc) < 4.9f
											select x;
			List<Thing> list = enumerable.ToList();
			for (int j = 0; j < list.Count; j++)
			{
				list[j].Destroy(DestroyMode.Vanish);
			}
			int randomInRange = BloodFilthRange.RandomInRange;
			for (int i = 0; i < randomInRange; i++)
			{
				if (CellFinder.TryFindRandomCellNear(loc, map, 4, (IntVec3 c) => c.Standable(map), out var result))
				{
					FilthMaker.TryMakeFilth(result, map, ThingDefOf.Filth_MachineBits);
				}
			}
		}
	}
}
