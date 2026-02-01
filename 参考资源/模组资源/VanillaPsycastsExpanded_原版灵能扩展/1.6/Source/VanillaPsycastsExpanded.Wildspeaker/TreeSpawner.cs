using System.Linq;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class TreeSpawner : PlantSpawner
{
	protected override int DurationTicks()
	{
		return 5f.SecondsToTicks();
	}

	protected override ThingDef ChoosePlant(IntVec3 loc, Map map)
	{
		if ((map.Biome.AllWildPlants.Where((ThingDef td) => td.plant?.IsTree ?? false).TryRandomElement(out var result) || map.Biome.AllWildPlants.TryRandomElement(out result)) && result.CanEverPlantAt(loc, map, canWipePlantsExceptTree: true) && PlantUtility.AdjacentSowBlocker(result, loc, map) == null)
		{
			return result;
		}
		return null;
	}

	protected override void SetupPlant(Plant plant, IntVec3 loc, Map map)
	{
		if (PlantUtility.AdjacentSowBlocker(plant.def, loc, map) != null)
		{
			plant.Destroy();
		}
		else
		{
			plant.Growth = 1f;
		}
	}
}
