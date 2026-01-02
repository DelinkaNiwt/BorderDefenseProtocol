using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

public class WorldComponent_GenerateLocation : WorldComponent
{
	private static readonly Dictionary<PlanetLayerDef, List<GeneratedLocationDef_Custom>> LayerList = new Dictionary<PlanetLayerDef, List<GeneratedLocationDef_Custom>>();

	private static readonly List<GeneratedLocationDef_Custom> tmpOptions = new List<GeneratedLocationDef_Custom>();

	public WorldComponent_GenerateLocation(World world)
		: base(world)
	{
		LayerList.Clear();
		foreach (GeneratedLocationDef_Custom item in DefDatabase<GeneratedLocationDef_Custom>.AllDefsListForReading)
		{
			PlanetLayerDef layerDef = item.LayerDef;
			if (!LayerList.TryGetValue(layerDef, out var value))
			{
				List<GeneratedLocationDef_Custom> list = (LayerList[layerDef] = new List<GeneratedLocationDef_Custom>());
				value = list;
			}
			value.Add(item);
		}
	}

	public override void FinalizeInit(bool fromLoad)
	{
		if (!fromLoad)
		{
			GenerateFromDefs();
		}
	}

	public void GenerateFromDefs()
	{
		foreach (KeyValuePair<int, PlanetLayer> planetLayer2 in Find.WorldGrid.PlanetLayers)
		{
			var (_, layer) = planetLayer2;
			GenerateFromDefs(layer);
		}
	}

	public static bool TryFindSiteTile(PlanetLayer layer, out PlanetTile tile)
	{
		tile = PlanetTile.Invalid;
		PlanetTile playerReferenceTile = GetPlayerReferenceTile();
		return TileFinder.TryFindTileWithDistance(layer.GetClosestTile(playerReferenceTile), 0, int.MaxValue, out tile, IsTileValidForSite);
	}

	private static PlanetTile GetPlayerReferenceTile()
	{
		if (TileFinder.TryFindRandomPlayerTile(out var tile, allowCaravans: false))
		{
			return tile;
		}
		return new PlanetTile(0, Find.WorldGrid.Surface);
	}

	private static bool IsTileValidForSite(PlanetTile tile)
	{
		return !Find.WorldObjects.AnyWorldObjectAt(tile);
	}

	public void GenerateFromDefs(PlanetLayer layer)
	{
		if (!LayerList.ContainsKey(layer.Def) || LayerList[layer.Def].Empty() || !TryFindSiteTile(layer, out var tile))
		{
			return;
		}
		tmpOptions.Clear();
		tmpOptions.AddRange(LayerList[layer.Def]);
		foreach (GeneratedLocationDef_Custom location in LayerList[layer.Def])
		{
			if (location.layerMaximum >= 0 && layer.Tiles.Count((Tile t) => world.worldObjects.WorldObjectOfDefAt(location.worldObjectDef, t.tile) != null) >= location.layerMaximum)
			{
				tmpOptions.Remove(location);
			}
		}
		if (tmpOptions.Empty())
		{
			return;
		}
		foreach (GeneratedLocationDef_Custom tmpOption in tmpOptions)
		{
			int num = Rand.Range(tmpOption.objectAmount.min, tmpOption.objectAmount.max);
			if (num <= 0)
			{
				continue;
			}
			for (int num2 = 0; num2 < num; num2++)
			{
				Log.Message(num2.ToString() + num);
				WorldObject worldObject = WorldObjectMaker.MakeWorldObject(tmpOption.worldObjectDef);
				if (worldObject is INameableWorldObject nameableWorldObject)
				{
					nameableWorldObject.Name = NameGenerator.GenerateName(worldObject.def.nameMaker, null, appendNumberIfNameUsed: false, "r_name", null, null);
				}
				if (worldObject is IResourceWorldObject resourceWorldObject && !tmpOption.preciousResources.NullOrEmpty())
				{
					resourceWorldObject.PreciousResource = tmpOption.preciousResources.RandomElement();
				}
				if (worldObject is IExpirableWorldObject expirableWorldObject && !tmpOption.TimeoutRangeDays.IsZeros)
				{
					expirableWorldObject.ExpireAtTicks = GenTicks.TicksGame + (int)(tmpOption.TimeoutRangeDays.RandomInRange * 60000f);
				}
				worldObject.isGeneratedLocation = true;
				worldObject.Tile = tile;
				world.worldObjects.Add(worldObject);
				if (!TryFindSiteTile(layer, out tile))
				{
					return;
				}
			}
		}
	}
}
