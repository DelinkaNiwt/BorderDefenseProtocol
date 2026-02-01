using Verse;

namespace NCL;

public class PlaceWorker_MustBeRoofed : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		Room room = loc.GetRoom(map);
		if (room != null && (room.OutdoorsForWork || !map.roofGrid.Roofed(loc)))
		{
			return new AcceptanceReport("NCL_MustPlaceRoofed".Translate());
		}
		return true;
	}
}
