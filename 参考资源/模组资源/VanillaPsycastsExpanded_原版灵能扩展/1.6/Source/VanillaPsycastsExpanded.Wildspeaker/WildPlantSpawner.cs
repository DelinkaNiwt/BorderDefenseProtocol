using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class WildPlantSpawner : PlantSpawner
{
	protected override ThingDef ChoosePlant(IntVec3 loc, Map map)
	{
		if (Rand.Chance(0.2f))
		{
			return null;
		}
		if (DefDatabase<ThingDef>.AllDefs.Where(delegate(ThingDef td)
		{
			PlantProperties plant = td.plant;
			return plant != null && plant.Sowable && !plant.IsTree;
		}).TryRandomElement(out var result) && result.CanEverPlantAt(loc, map, canWipePlantsExceptTree: true))
		{
			return result;
		}
		return null;
	}

	protected override void SetupPlant(Plant plant, IntVec3 loc, Map map)
	{
		base.SetupPlant(plant, loc, map);
		plant.Growth = Mathf.Clamp(((Thing)this).TryGetComp<CompAbilitySpawn>().pawn.GetStatValue(StatDefOf.PsychicSensitivity) - 1f, 0.1f, 1f);
	}
}
