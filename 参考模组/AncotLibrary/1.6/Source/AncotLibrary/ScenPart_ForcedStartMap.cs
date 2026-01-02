using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class ScenPart_ForcedStartMap : ScenPart
{
	public MapGeneratorDef mapGenerator;

	public PlanetLayerDef layerDef;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref mapGenerator, "mapGenerator");
		Scribe_Defs.Look(ref layerDef, "layerDef");
	}

	public override string Summary(Scenario scen)
	{
		string key = "Ancot.ScenPart_ForcedStartMap";
		return ScenSummaryList.SummaryWithList(scen, "Ancot.ScenPart_ForcedStartMap", key.Translate());
	}

	public override IEnumerable<string> GetSummaryListEntries(string tag)
	{
		if (tag == "Ancot.ScenPart_ForcedStartMap")
		{
			yield return ((string)mapGenerator.LabelCap != null) ? mapGenerator.LabelCap : ((TaggedString)mapGenerator.defName);
		}
	}

	public override void DoEditInterface(Listing_ScenEdit listing)
	{
		Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 2f);
		scenPartRect.height = ScenPart.RowHeight;
		Text.Anchor = TextAnchor.UpperRight;
		Rect rect = new Rect(scenPartRect.x - 200f, scenPartRect.y + ScenPart.RowHeight, 200f, ScenPart.RowHeight);
		rect.xMax -= 4f;
		Widgets.Label(rect, "ScenPart_ForcedMapPlanetLayer".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
		if (Widgets.ButtonText(scenPartRect, mapGenerator.LabelCap))
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			foreach (MapGeneratorDef item in DefDatabase<MapGeneratorDef>.AllDefs.Where((MapGeneratorDef d) => d.validScenarioMap))
			{
				MapGeneratorDef localFd2 = item;
				list.Add(new FloatMenuOption(localFd2.LabelCap, delegate
				{
					mapGenerator = localFd2;
				}));
			}
			Find.WindowStack.Add(new FloatMenu(list));
		}
		scenPartRect.y += ScenPart.RowHeight;
		if (!Widgets.ButtonText(scenPartRect, layerDef.LabelCap))
		{
			return;
		}
		List<FloatMenuOption> list2 = new List<FloatMenuOption>();
		foreach (PlanetLayerDef allDef in DefDatabase<PlanetLayerDef>.AllDefs)
		{
			PlanetLayerDef localFd3 = allDef;
			list2.Add(new FloatMenuOption(localFd3.LabelCap, delegate
			{
				layerDef = localFd3;
			}));
		}
		Find.WindowStack.Add(new FloatMenu(list2));
	}

	public override void PostWorldGenerate()
	{
		PlanetTile planetTile = TileFinder.RandomStartingTile();
		PlanetLayer planetLayer = Find.WorldGrid.FirstLayerOfDef(layerDef);
		if (layerDef != PlanetLayerDefOf.Surface && planetLayer != null)
		{
			planetTile = planetLayer.GetClosestTile(planetTile);
		}
		Find.GameInitData.startingTile = planetTile;
		Find.GameInitData.mapGeneratorDef = mapGenerator;
	}
}
