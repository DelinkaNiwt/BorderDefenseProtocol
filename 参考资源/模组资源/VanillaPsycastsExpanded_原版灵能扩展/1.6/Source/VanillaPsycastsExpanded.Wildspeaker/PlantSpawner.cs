using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded.Wildspeaker;

[HarmonyPatch]
public abstract class PlantSpawner : GroundSpawner
{
	private ThingDef plantDef;

	protected override void Spawn(Map map, IntVec3 loc)
	{
		if (plantDef != null)
		{
			Plant plant = (Plant)GenSpawn.Spawn(plantDef, loc, map);
			SetupPlant(plant, loc, map);
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			secondarySpawnTick = Find.TickManager.TicksGame + DurationTicks() + Rand.RangeInclusive(-60, 120);
			filthSpawnMTB = float.PositiveInfinity;
			Rand.PushState(Find.TickManager.TicksGame);
			plantDef = ChoosePlant(base.Position, map);
			Rand.PopState();
		}
		if (!CheckSpawnLoc(base.Position, map) || plantDef == null)
		{
			Destroy();
		}
	}

	protected virtual bool CheckSpawnLoc(IntVec3 loc, Map map)
	{
		if (loc.GetTerrain(map).fertility == 0f)
		{
			return false;
		}
		List<Thing> thingList = loc.GetThingList(map);
		for (int num = thingList.Count - 1; num >= 0; num--)
		{
			Thing thing = thingList[num];
			if (thing is Plant)
			{
				if (thing.def.plant.IsTree)
				{
					return false;
				}
				thing.Destroy();
			}
			if (thing.def.IsEdifice())
			{
				return false;
			}
		}
		return true;
	}

	protected abstract ThingDef ChoosePlant(IntVec3 loc, Map map);

	protected virtual void SetupPlant(Plant plant, IntVec3 loc, Map map)
	{
	}

	protected virtual int DurationTicks()
	{
		return 3f.SecondsToTicks();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref plantDef, "plantDef");
	}
}
