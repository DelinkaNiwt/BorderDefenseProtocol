using Verse;

namespace NCL;

public class PlaceWorker_ShowAntiInvisibilityRadius : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		GenDraw.DrawRadiusRing(loc, (checkingDef as ThingDef).GetCompProperties<CompProperties_AntiInvisibilityField>().effectiveRadius);
		return true;
	}
}
