using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

public class ThingSetMaker_TraderStockCustom : ThingSetMaker_TraderStock
{
	public List<ThingDef> thingsIgnoreTradeability;

	protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
	{
		TraderKindDef traderKindDef = parms.traderDef ?? DefDatabase<TraderKindDef>.AllDefsListForReading.RandomElement();
		Faction makingFaction = parms.makingFaction;
		PlanetTile forTile = (parms.tile.HasValue ? parms.tile.Value : ((Find.AnyPlayerHomeMap != null) ? Find.AnyPlayerHomeMap.Tile : ((Find.CurrentMap == null) ? PlanetTile.Invalid : Find.CurrentMap.Tile)));
		for (int i = 0; i < traderKindDef.stockGenerators.Count; i++)
		{
			foreach (Thing item in traderKindDef.stockGenerators[i].GenerateThings(forTile, parms.makingFaction))
			{
				if (!item.def.tradeability.TraderCanSell() && !thingsIgnoreTradeability.Contains(item.def))
				{
					Log.Error(traderKindDef?.ToString() + " generated carrying " + item?.ToString() + " which can't be sold by traders. Ignoring...");
					continue;
				}
				item.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
				outThings.Add(item);
			}
		}
	}
}
