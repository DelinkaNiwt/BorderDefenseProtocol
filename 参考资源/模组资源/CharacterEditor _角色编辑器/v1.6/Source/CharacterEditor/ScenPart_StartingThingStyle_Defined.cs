using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CharacterEditor;

public class ScenPart_StartingThingStyle_Defined : ScenPart_ThingCount
{
	public IntVec3 location;

	public ThingStyleDef styleDef;

	public const string PlayerStartWithTag = "PlayerStartsWith";

	public static string PlayerStartWithIntro => "ScenPart_StartWith".Translate();

	public override string Summary(Scenario scen)
	{
		return ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith", PlayerStartWithIntro);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref styleDef, "styleDef");
	}

	public override IEnumerable<string> GetSummaryListEntries(string tag)
	{
		if (tag == "PlayerStartsWith")
		{
			yield return GenLabel.ThingLabel(thingDef, stuff, count).CapitalizeFirst();
		}
	}

	public override IEnumerable<Thing> PlayerStartingThings()
	{
		Thing thing = ThingMaker.MakeThing(thingDef, stuff);
		if (quality.HasValue)
		{
			thing.TryGetComp<CompQuality>()?.SetQuality(quality.Value, ArtGenerationContext.Outsider);
		}
		if (thingDef.Minifiable)
		{
			thing = thing.MakeMinified();
		}
		if (styleDef != null)
		{
			thing.StyleDef = styleDef;
		}
		if (thingDef.ingestible != null && thingDef.IsIngestible && thingDef.ingestible.IsMeal)
		{
			FoodUtility.GenerateGoodIngredients(thing, Faction.OfPlayer.ideos.PrimaryIdeo);
		}
		thing.stackCount = count;
		yield return thing;
	}
}
