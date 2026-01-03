using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class PlaceWorker_SAMSite : PlaceWorker
{
	public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
	{
		GenDraw.DrawRadiusRing(center, 39.9f, Color.cyan);
		GenDraw.DrawRadiusRing(center, 9f, Color.cyan);
	}
}
