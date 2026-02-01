using Verse;

namespace TowerLaserDefense;

public class PlaceWorker_ShowTurretRadius : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		GenDraw.DrawRadiusRing(loc, (checkingDef as ThingDef).GetCompProperties<CompProperties_LaserDefence>().laserDefenceProperties.range);
		return true;
	}
}
