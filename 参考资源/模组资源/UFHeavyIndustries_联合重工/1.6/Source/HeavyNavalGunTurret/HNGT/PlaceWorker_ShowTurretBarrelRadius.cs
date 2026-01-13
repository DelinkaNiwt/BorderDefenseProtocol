using Verse;

namespace HNGT;

public class PlaceWorker_ShowTurretBarrelRadius : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		VerbProperties verbProperties = ((ThingDef)checkingDef).building.turretGunDef.Verbs.Find((VerbProperties v) => v.verbClass == typeof(Verb_BarrelWithRecoilAndFlash));
		if (verbProperties.range > 0f)
		{
			GenDraw.DrawRadiusRing(loc, verbProperties.range);
		}
		if (verbProperties.minRange > 0f)
		{
			GenDraw.DrawRadiusRing(loc, verbProperties.minRange);
		}
		return true;
	}
}
