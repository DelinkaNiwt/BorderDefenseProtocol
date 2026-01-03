using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class CompProperties_LeavingItemDestroyed : CompProperties
{
	public bool onlyNonPlayerLeaving;

	public List<ThingDefWithCommonality> thingDefWithCommonalities;

	public CompProperties_LeavingItemDestroyed()
	{
		compClass = typeof(CompLeavingItemDestroyed);
	}
}
