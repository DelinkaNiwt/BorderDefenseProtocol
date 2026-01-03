using Verse;

namespace AncotLibrary;

public class PlaceWorker_OnlyOneInMapColonist : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		if (map.listerThings.AllThings.ContainsAny((Thing t) => t.def == checkingDef || t.def.entityDefToBuild == checkingDef))
		{
			return new AcceptanceReport("Ancot.OnlyOneInMap".Translate());
		}
		return true;
	}
}
