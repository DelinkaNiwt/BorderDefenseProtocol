using RimWorld.BaseGen;
using Verse;

namespace Milira;

public class SymbolResolver_SunLightFuelStation_Integration : SymbolResolver
{
	public override void Resolve(ResolveParams rp)
	{
		float value = Rand.Value;
		ResolveParams resolveParams = rp;
		resolveParams.rect = resolveParams.rect.ContractedBy(2);
		BaseGen.symbolStack.Push("milira_SunLightFuelStation", resolveParams);
		BaseGen.symbolStack.Push("ensureCanReachMapEdge", rp);
		ResolveParams resolveParams2 = rp;
		resolveParams2.clearFillageOnly = true;
		resolveParams2.clearRoof = true;
		BaseGen.symbolStack.Push("clear", resolveParams2);
	}
}
