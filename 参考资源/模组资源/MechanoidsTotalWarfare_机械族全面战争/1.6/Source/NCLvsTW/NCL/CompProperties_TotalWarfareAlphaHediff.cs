using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompProperties_TotalWarfareAlphaHediff : CompProperties
{
	public List<HediffDef> TWhediffsRange;

	public List<HediffDef> TWhediffsMelee;

	public CompProperties_TotalWarfareAlphaHediff()
	{
		compClass = typeof(CompTotalWarfareAlphaHediff);
	}
}
