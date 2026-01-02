using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class PlaceWorker_NotCrossClose : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		List<IntVec3> list = new List<IntVec3>();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(center, 1f, useCenter: true))
		{
			list.Add(item);
		}
		foreach (IntVec3 item2 in list)
		{
			if (!item2.InBounds(map))
			{
				continue;
			}
			List<Thing> thingList = item2.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				if (thingList[i].def == def || thingList[i].def.entityDefToBuild == def)
				{
					return "MustNotBePlacedCloseToAnother".Translate(def);
				}
			}
		}
		return true;
	}
}
