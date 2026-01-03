using Verse;

namespace TOT_DLL_test;

public class PlaceWorker_ShowTurretRadius_VerbSupport : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		VerbProperties verbProperties = ((ThingDef)checkingDef).building.turretGunDef.Verbs.Find((VerbProperties v) => v.verbClass == typeof(Verb_LaunchProjectile) || typeof(Verb_LaserShoot).IsAssignableFrom(v.verbClass) || typeof(Verb_ShootMultiTarget).IsAssignableFrom(v.verbClass) || typeof(Verb_RocketShoot).IsAssignableFrom(v.verbClass) || typeof(Verb_ShootSwitchFire).IsAssignableFrom(v.verbClass));
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
