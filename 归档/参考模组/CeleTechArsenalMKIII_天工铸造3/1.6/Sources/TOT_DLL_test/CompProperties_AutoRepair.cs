using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_AutoRepair : CompProperties
{
	public int ticksPerHeal;

	public CompProperties_AutoRepair()
	{
		compClass = typeof(CompAutoRepair);
	}

	public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
	{
		foreach (string item in base.ConfigErrors(parentDef))
		{
			yield return item;
		}
		if (parentDef.tickerType == TickerType.Rare && ticksPerHeal % 250 != 0)
		{
			yield return "TickerType is set to Rare, but ticksPerHeal value is not multiple of " + 250;
		}
		if (parentDef.tickerType == TickerType.Long && ticksPerHeal % 1200 != 0)
		{
			yield return "TickerType is set to Long, but ticksPerHeal value is not multiple of " + 2000;
		}
		if (parentDef.tickerType == TickerType.Never)
		{
			yield return "has CompSelfhealHitpoints, but its TickerType is set to Never";
		}
	}
}
