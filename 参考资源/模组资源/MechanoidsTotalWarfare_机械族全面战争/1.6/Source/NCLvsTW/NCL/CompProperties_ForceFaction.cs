using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_ForceFaction : CompProperties
{
	public FactionDef factionDef;

	public CompProperties_ForceFaction()
	{
		compClass = typeof(CompForceFaction);
	}
}
