using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_Trader : CompProperties
{
	public string traderDefName;

	public virtual Tradeable CreateEmployTradeable()
	{
		return new Tradeable_MechanoidEmploy();
	}

	public CompProperties_Trader()
	{
		compClass = typeof(CompTrader);
	}
}
