using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Harmonist;

public class Ability_TransmuteStone : Ability
{
	private static readonly AccessTools.FieldRef<World, List<ThingDef>> allNaturalRockDefs = AccessTools.FieldRefAccess<World, List<ThingDef>>("allNaturalRockDefs");

	private static readonly AccessTools.FieldRef<Thing, Graphic> graphicInt = AccessTools.FieldRefAccess<Thing, Graphic>("graphicInt");

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			Map map = globalTargetInfo.Map;
			Find.World.NaturalRockTypesIn(map.Tile);
			List<ThingDef> naturalRockDefs = allNaturalRockDefs(Find.World);
			ThingDef chosenRock = naturalRockDefs.RandomElement();
			foreach (IntVec3 item in GenRadial.RadialCellsAround(globalTargetInfo.Cell, ((Ability)this).GetRadiusForPawn(), useCenter: true))
			{
				foreach (Thing item2 in item.GetThingList(map).ListFullCopy())
				{
					if (item2.def.IsNonResourceNaturalRock)
					{
						Replace(item2, chosenRock);
						continue;
					}
					foreach (ThingDef item3 in naturalRockDefs)
					{
						if (item3.building.mineableThing != null && item3.building.mineableThing == item2.def)
						{
							Replace(item2, chosenRock.building.mineableThing);
						}
						else if (item3.building.smoothedThing != null && item3.building.smoothedThing == item2.def)
						{
							Replace(item2, chosenRock.building.smoothedThing);
						}
						else if (item3.building.mineableThing?.butcherProducts[0]?.thingDef == item2.def)
						{
							Replace(item2, chosenRock.building.mineableThing.butcherProducts[0].thingDef);
						}
						else if (item2.Stuff != null && item2.Stuff == item3.building.mineableThing?.butcherProducts[0]?.thingDef)
						{
							Replace(item2, null, chosenRock.building.mineableThing.butcherProducts[0].thingDef);
						}
					}
				}
				TerrainGrid terrainGrid = map.terrainGrid;
				TerrainDef terrain = terrainGrid.TerrainAt(item);
				terrainGrid.SetTerrain(item, NewTerrain(terrain));
				terrain = terrainGrid.UnderTerrainAt(item);
				if (terrain != null)
				{
					terrainGrid.SetUnderTerrain(item, NewTerrain(terrain));
				}
			}
			TerrainDef NewTerrain(TerrainDef terrainDef)
			{
				string text = terrainDef.defName;
				foreach (ThingDef item4 in naturalRockDefs.Except(chosenRock))
				{
					if (text.StartsWith(item4.defName))
					{
						text = text.Replace(item4.defName, chosenRock.defName);
					}
				}
				return TerrainDef.Named(text);
			}
			void Replace(Thing thing, ThingDef def = null, ThingDef stuff = null)
			{
				ThingOwner holdingOwner = thing.holdingOwner;
				IntVec3 position = thing.Position;
				Rot4 rotation = thing.Rotation;
				if (def == null)
				{
					def = thing.def;
				}
				if (stuff == null)
				{
					stuff = thing.Stuff;
				}
				Thing thing2 = ThingMaker.MakeThing(def, stuff);
				List<Designation> list = map.designationManager.AllDesignationsOn(thing).ListFullCopy();
				thing.Destroy();
				if (position.IsValid)
				{
					GenSpawn.Spawn(thing2, position, map, rotation);
				}
				else
				{
					if (holdingOwner == null)
					{
						Log.Warning($"[VPE] Attempting to replace unspawned and unheld thing {thing}");
						return;
					}
					if (!holdingOwner.TryAdd(thing2))
					{
						Log.Error($"[VPE] Failed to add {thing2} to {holdingOwner}");
					}
				}
				foreach (Designation item5 in list)
				{
					map.designationManager.AddDesignation(new Designation(thing2, item5.def));
				}
			}
		}
	}
}
