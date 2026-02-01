using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompProperties_TotalWarfareBetaHediff : CompProperties
{
	public List<HediffDef> TWhediffsRange;

	public List<HediffDef> TWhediffsMelee;

	public CompProperties_TotalWarfareBetaHediff()
	{
		compClass = typeof(CompTotalWarfareBetaHediff);
	}
}
