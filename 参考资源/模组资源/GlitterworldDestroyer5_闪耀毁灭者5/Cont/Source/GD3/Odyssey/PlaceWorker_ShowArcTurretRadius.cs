using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
	public class PlaceWorker_ShowArcTurretRadius : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			VerbProperties verbProperties = ((ThingDef)checkingDef).building.turretGunDef.Verbs.Find((VerbProperties v) => typeof(Verb_ThrowArc).IsAssignableFrom(v.verbClass));
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

}
