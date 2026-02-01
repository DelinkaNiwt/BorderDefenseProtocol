using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NCL;

public class StockGenerator_BuyingMetalWithEmploy : StockGenerator
{
	public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
	{
		yield break;
	}

	public override bool HandlesThingDef(ThingDef t)
	{
		return t?.stuffProps?.categories.Contains(StuffCategoryDefOf.Metallic) == true || t == ThingDefOf.Silver;
	}

	public override Tradeability TradeabilityFor(ThingDef thingDef)
	{
		return HandlesThingDef(thingDef) ? Tradeability.All : Tradeability.None;
	}
}
