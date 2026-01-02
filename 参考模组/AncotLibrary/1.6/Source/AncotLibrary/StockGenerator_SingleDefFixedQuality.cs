using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

public class StockGenerator_SingleDefFixedQuality : StockGenerator
{
	public ThingDef thingDef;

	public QualityCategory quality;

	public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
	{
		foreach (Thing item in StockGeneratorUtility_Custom.TryMakeForStock(thingDef, RandomCountOf(thingDef), faction, quality))
		{
			CompQuality comp = item.TryGetComp<CompQuality>();
			if (comp != null)
			{
				Log.Message("111");
			}
			comp.SetQuality(quality, null);
			item.TryGetQuality(out var qc);
			Log.Message("quality" + qc);
			yield return item;
		}
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
