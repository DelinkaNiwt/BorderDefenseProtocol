using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class GenStep_PlantGrowth : GenStep
{
	public List<ThingDef> plants = new List<ThingDef>();

	public float targetGrowth = 0.75f;

	public float growthOffset = 0.1f;

	public override int SeedPart => 123456329;

	public override void Generate(Map map, GenStepParams parms)
	{
		List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.Plant).ToList();
		foreach (Thing item in list)
		{
			if (item is Plant plant && plants.Contains(plant.def))
			{
				TerrainDef terrain = plant.Position.GetTerrain(map);
				if (!plant.def.plant.sowTags.NullOrEmpty() && terrain.fertility < plant.def.plant.fertilityMin)
				{
					plant.Destroy();
				}
				else
				{
					plant.Growth = Mathf.Clamp(Rand.Range(targetGrowth - growthOffset, targetGrowth + growthOffset), 0f, 1f);
				}
			}
		}
	}
}
