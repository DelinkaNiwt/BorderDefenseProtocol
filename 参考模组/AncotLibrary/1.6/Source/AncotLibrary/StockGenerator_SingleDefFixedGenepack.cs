using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

public class StockGenerator_SingleDefFixedGenepack : StockGenerator
{
	private List<GeneDef> genes;

	private ThingDef thingDef = ThingDefOf.Genepack;

	public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
	{
		foreach (Thing item in StockGeneratorUtility.TryMakeForStock(thingDef, RandomCountOf(thingDef), faction))
		{
			if (item is Genepack genepack)
			{
				FieldInfo field = typeof(Genepack).GetField("geneSet", BindingFlags.Instance | BindingFlags.NonPublic);
				GeneSet geneSet = GenerateGeneSet();
				field.SetValue(genepack, geneSet);
			}
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
			yield return thingDef?.ToString() + " tradeability doesn't allow traders to sell this thing";
		}
	}

	public GeneSet GenerateGeneSet()
	{
		if (!ModLister.CheckBiotech("geneset generation"))
		{
			return null;
		}
		GeneSet geneSet = new GeneSet();
		for (int i = 0; i < genes.Count; i++)
		{
			geneSet.AddGene(genes[i]);
		}
		geneSet.GenerateName();
		if (geneSet.Empty)
		{
			Log.Error("Generated gene pack with no genes.");
		}
		geneSet.SortGenes();
		return geneSet;
	}
}
