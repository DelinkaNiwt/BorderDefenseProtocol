using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class StockGeneratorUtility_Custom
{
	public static IEnumerable<Thing> TryMakeForStock(ThingDef thingDef, int count, Faction faction)
	{
		if (thingDef.MadeFromStuff || thingDef.tradeNeverStack || thingDef.tradeNeverGenerateStacked)
		{
			for (int i = 0; i < count; i++)
			{
				Thing thing = TryMakeForStockSingle(thingDef, 1, faction);
				if (thing != null)
				{
					yield return thing;
				}
			}
		}
		else
		{
			Thing thing2 = TryMakeForStockSingle(thingDef, count, faction);
			if (thing2 != null)
			{
				yield return thing2;
			}
		}
	}

	public static IEnumerable<Thing> TryMakeForStock(ThingDef thingDef, int count, Faction faction, ThingDef stuff)
	{
		if (thingDef.MadeFromStuff || thingDef.tradeNeverStack || thingDef.tradeNeverGenerateStacked)
		{
			for (int i = 0; i < count; i++)
			{
				Thing thing = TryMakeForStockSingle(thingDef, 1, faction, stuff);
				if (thing != null)
				{
					yield return thing;
				}
			}
		}
		else
		{
			Thing thing2 = TryMakeForStockSingle(thingDef, count, faction, stuff);
			if (thing2 != null)
			{
				yield return thing2;
			}
		}
	}

	public static IEnumerable<Thing> TryMakeForStock(ThingDef thingDef, int count, Faction faction, List<ThingDef> stuffCategory)
	{
		if (thingDef.MadeFromStuff || thingDef.tradeNeverStack || thingDef.tradeNeverGenerateStacked)
		{
			for (int i = 0; i < count; i++)
			{
				Thing thing = TryMakeForStockSingle(thingDef, 1, faction, stuffCategory.RandomElementWithFallback());
				if (thing != null)
				{
					yield return thing;
				}
			}
		}
		else
		{
			Thing thing2 = TryMakeForStockSingle(thingDef, count, faction, stuffCategory.RandomElementWithFallback());
			if (thing2 != null)
			{
				yield return thing2;
			}
		}
	}

	public static IEnumerable<Thing> TryMakeForStock(ThingDef thingDef, int count, Faction faction, QualityCategory quality)
	{
		if (thingDef.MadeFromStuff || thingDef.tradeNeverStack || thingDef.tradeNeverGenerateStacked)
		{
			for (int i = 0; i < count; i++)
			{
				Thing thing = StockGeneratorUtility.TryMakeForStockSingle(thingDef, 1, faction);
				if (thing != null)
				{
					thing.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Colony);
					yield return thing;
				}
			}
		}
		else
		{
			Thing thing2 = StockGeneratorUtility.TryMakeForStockSingle(thingDef, count, faction);
			if (thing2 != null)
			{
				thing2.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Colony);
				yield return thing2;
			}
		}
	}

	public static Thing TryMakeForStockSingle(ThingDef thingDef, int stackCount, Faction faction)
	{
		if (stackCount <= 0)
		{
			return null;
		}
		ThingDef result = null;
		if (thingDef.MadeFromStuff && !(from x in GenStuff.AllowedStuffsFor(thingDef, TechLevel.Undefined, checkAllowedInStuffGeneration: true)
			where !PawnWeaponGenerator.IsDerpWeapon(thingDef, x)
			select x).TryRandomElementByWeight((ThingDef x) => x.stuffProps.commonality, out result))
		{
			result = GenStuff.RandomStuffByCommonalityFor(thingDef);
		}
		Thing thing = ThingMaker.MakeThing(thingDef, result);
		thing.stackCount = stackCount;
		return thing;
	}

	public static Thing TryMakeForStockSingle(ThingDef thingDef, int stackCount, Faction faction, ThingDef stuff = null)
	{
		if (stackCount <= 0)
		{
			return null;
		}
		ThingDef result = null;
		if (stuff == null && stuff.stuffProps.CanMake(thingDef) && thingDef.MadeFromStuff && !(from x in GenStuff.AllowedStuffsFor(thingDef)
			where !PawnWeaponGenerator.IsDerpWeapon(thingDef, x)
			select x).TryRandomElementByWeight((ThingDef x) => x.stuffProps.commonality, out result))
		{
			result = GenStuff.RandomStuffByCommonalityFor(thingDef);
		}
		if (stuff != null)
		{
			result = stuff;
		}
		Thing thing = ThingMaker.MakeThing(thingDef, result);
		thing.stackCount = stackCount;
		return thing;
	}
}
