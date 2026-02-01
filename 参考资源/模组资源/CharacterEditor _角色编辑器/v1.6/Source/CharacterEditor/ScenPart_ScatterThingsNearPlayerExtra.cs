using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal class ScenPart_ScatterThingsNearPlayerExtra : ScenPart_ThingCount
{
	public IntVec3 location;

	public ThingStyleDef styleDef;

	public bool allowRoofed = true;

	protected bool NearPlayerStart => true;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref styleDef, "styleDef");
		Scribe_Values.Look(ref allowRoofed, "allowRoofed", defaultValue: false);
	}

	public override string Summary(Scenario scen)
	{
		return ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith", ScenPart_StartingThing_Defined.PlayerStartWithIntro);
	}

	public override IEnumerable<string> GetSummaryListEntries(string tag)
	{
		if (tag == "PlayerStartsWith")
		{
			yield return GenLabel.ThingLabel(thingDef, stuff, count).CapitalizeFirst();
		}
	}

	public override void GenerateIntoMap(Map map)
	{
		if (Find.GameInitData != null)
		{
			GenStep_ScatterThings2 genStep_ScatterThings = new GenStep_ScatterThings2();
			genStep_ScatterThings.nearPlayerStart = NearPlayerStart;
			genStep_ScatterThings.allowFoggedPositions = !NearPlayerStart;
			genStep_ScatterThings.thingDef = thingDef;
			genStep_ScatterThings.stuff = stuff;
			genStep_ScatterThings.styleDef = styleDef;
			genStep_ScatterThings.count = count;
			genStep_ScatterThings.spotMustBeStandable = true;
			genStep_ScatterThings.minSpacing = 5f;
			genStep_ScatterThings.clusterSize = ((thingDef != null && thingDef.category == Verse.ThingCategory.Building) ? 1 : 4);
			genStep_ScatterThings.allowRoofed = allowRoofed;
			genStep_ScatterThings.Generate(map, default(GenStepParams));
		}
	}

	public override int GetHashCode()
	{
		return base.GetHashCode() ^ (allowRoofed ? 1 : 0);
	}
}
