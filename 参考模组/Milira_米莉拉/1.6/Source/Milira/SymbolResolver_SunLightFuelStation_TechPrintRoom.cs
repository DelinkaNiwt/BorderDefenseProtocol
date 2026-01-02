using RimWorld.BaseGen;
using Verse;

namespace Milira;

public class SymbolResolver_SunLightFuelStation_TechPrintRoom : SymbolResolver
{
	public override void Resolve(ResolveParams rp)
	{
		ThingDef singleThingDef = rp.singleThingDef;
		ResolveParams resolveParams = rp;
		resolveParams.singleThingDef = singleThingDef;
		resolveParams.skipSingleThingIfHasToWipeBuildingOrDoesntFit = rp.skipSingleThingIfHasToWipeBuildingOrDoesntFit ?? true;
		BaseGen.symbolStack.Push("thing", resolveParams);
	}
}
