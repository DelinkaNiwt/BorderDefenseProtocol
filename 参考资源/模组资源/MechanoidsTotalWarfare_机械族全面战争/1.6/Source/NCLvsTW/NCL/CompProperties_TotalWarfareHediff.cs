using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompProperties_TotalWarfareHediff : CompProperties
{
	public List<HediffDef> TWhediffsRange;

	public List<HediffDef> TWhediffsMelee;

	public CompProperties_TotalWarfareHediff()
	{
		compClass = typeof(CompTotalWarfareHediff);
	}
}
