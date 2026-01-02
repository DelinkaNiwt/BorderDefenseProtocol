using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

public class StockGenerator_SingleDefIgnoreTradeability : StockGenerator
{
	private ThingDef thingDef;

	public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
	{
		foreach (Thing item in StockGeneratorUtility_Custom.TryMakeForStock(thingDef, RandomCountOf(thingDef), faction))
		{
			yield return item;
		}
	}

	public override bool HandlesThingDef(ThingDef thingDef)
	{
		return thingDef == this.thingDef;
	}
}
