using Verse;

namespace AncotLibrary;

public class PlaceWorker_IndoorRoofed : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		if (map.roofGrid.Roofed(loc) && !loc.UsesOutdoorTemperature(map))
		{
			return true;
		}
		return new AcceptanceReport("Ancot.MustPlaceIndoorRoofed".Translate());
	}
}
