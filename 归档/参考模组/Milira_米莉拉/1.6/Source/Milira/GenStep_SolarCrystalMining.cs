using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class GenStep_SolarCrystalMining : GenStep
{
	public IntRange crystalRadius = new IntRange(10, 20);

	public IntRange crystalGroup = new IntRange(10, 20);

	public IntRange crystalInGroup = new IntRange(2, 5);

	private int MinRoomCells = 225;

	public override int SeedPart => 1623587423;

	public override void Generate(Map map, GenStepParams parms)
	{
		TraverseParms traverseParams = TraverseParms.For(TraverseMode.NoPassClosedDoors).WithFenceblocked(forceFenceblocked: true);
		if (!RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => x.Standable(map) && !x.Fogged(map) && map.reachability.CanReachMapEdge(x, traverseParams) && x.GetRoom(map).CellCount >= MinRoomCells, map, out var result))
		{
			return;
		}
		IEnumerable<IntVec3> collection = GenRadial.RadialCellsAround(result, crystalRadius.RandomInRange, useCenter: false);
		List<IntVec3> source = new List<IntVec3>(collection);
		int randomInRange = crystalGroup.RandomInRange;
		for (int num = 0; num < randomInRange; num++)
		{
			ThingDef milira_SolarCrystalDruse = MiliraDefOf.Milira_SolarCrystalDruse;
			IntVec3 center = source.RandomElement();
			int randomInRange2 = crystalInGroup.RandomInRange;
			foreach (IntVec3 item in GridShapeMaker.IrregularLump(center, map, randomInRange2))
			{
				if (item.InBounds(map) && item.IsValid)
				{
					GenSpawn.Spawn(milira_SolarCrystalDruse, item, map);
				}
			}
		}
	}
}
