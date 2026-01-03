using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

public class StockGenerator_SingleDefFixedStuff_Category : StockGenerator
{
	public ThingDef thingDef;

	public ThingCategoryDef stuffCategoryDef;

	public List<ThingDef> excludedThingDefs;

	public List<ThingCategoryDef> excludedCategories;

	public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
	{
		foreach (Thing item in StockGeneratorUtility_Custom.TryMakeForStock(thingDef, RandomCountOf(thingDef), faction, GetFilteredStuffList(thingDef)))
		{
			yield return item;
		}
	}

	public List<ThingDef> GetFilteredStuffList(ThingDef item)
	{
		return stuffCategoryDef.DescendantThingDefs.Where((ThingDef t) => t.tradeability.TraderCanSell() && (int)t.techLevel <= (int)maxTechLevelGenerate && t.stuffProps.CanMake(item) && (excludedThingDefs == null || !excludedThingDefs.Contains(t)) && (excludedCategories == null || !excludedCategories.Any((ThingCategoryDef c) => c.DescendantThingDefs.Contains(t)))).ToList();
	}

	public override bool HandlesThingDef(ThingDef thingDef)
	{
		return thingDef == this.thingDef;
	}

	public override IEnumerable<string> ConfigErrors(TraderKindDef parentDef)
	{
		foreach (string item in base.ConfigErrors(parentDef))
		{
			yield return item;
		}
		if (!thingDef.tradeability.TraderCanSell())
		{
			yield return string.Concat(thingDef, " tradeability doesn't allow traders to sell this thing");
		}
	}
}
