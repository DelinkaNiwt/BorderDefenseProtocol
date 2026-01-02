using RimWorld.BaseGen;
using Verse;

namespace Milira;

public class SymbolResolver_SunBlastFurnace : SymbolResolver
{
	public override void Resolve(ResolveParams rp)
	{
		ThingDef singleThingDef = rp.singleThingDef;
		ResolveParams resolveParams = rp;
		resolveParams.singleThingDef = singleThingDef;
		resolveParams.skipSingleThingIfHasToWipeBuildingOrDoesntFit = rp.skipSingleThingIfHasToWipeBuildingOrDoesntFit ?? true;
		BaseGen.symbolStack.Push("thing", resolveParams);
		for (int i = 0; i < Rand.Range(0, 3); i++)
		{
			ResolveParams resolveParams2 = rp;
			resolveParams2.singleThingDef = MiliraDefOf.Milira_SunBlasterBoosterJar;
			resolveParams2.skipSingleThingIfHasToWipeBuildingOrDoesntFit = rp.skipSingleThingIfHasToWipeBuildingOrDoesntFit ?? true;
			BaseGen.symbolStack.Push("thing", resolveParams2);
		}
	}
}
