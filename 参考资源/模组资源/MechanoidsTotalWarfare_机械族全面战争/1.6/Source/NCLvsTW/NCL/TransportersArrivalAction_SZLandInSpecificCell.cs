using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NCL;

public class TransportersArrivalAction_SZLandInSpecificCell : TransportersArrivalAction
{
	private MapParent mapParent;

	private IntVec3 cell;

	private ThingDef landingPodDef;

	private ThingDef activeTransporterDef;

	public override bool GeneratesMap => false;

	public TransportersArrivalAction_SZLandInSpecificCell(MapParent mapParent, IntVec3 cell, ThingDef landingPodDef, ThingDef activeTransporterDef)
	{
		this.mapParent = mapParent;
		this.cell = cell;
		this.landingPodDef = landingPodDef;
		this.activeTransporterDef = activeTransporterDef;
	}

	public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
	{
		Map map = mapParent.Map;
		if (map == null)
		{
			Log.Error("Destination map not found for tile: " + tile.Tile);
			return;
		}
		for (int i = 0; i < transporters.Count; i++)
		{
			Thing landingPodThing = ThingMaker.MakeThing(landingPodDef);
			if (!(landingPodThing is Skyfaller landingPod))
			{
				Log.Error("Failed to create landing pod");
				continue;
			}
			Thing transporterThing = ThingMaker.MakeThing(activeTransporterDef);
			ActiveTransporter activeTransporter = transporterThing as ActiveTransporter;
			activeTransporter.Contents = transporters[i];
			if (!landingPod.innerContainer.TryAdd(activeTransporter))
			{
				Log.Error("Failed to add transporter to landing pod");
			}
			else
			{
				GenSpawn.Spawn(landingPod, cell, map);
			}
		}
	}

	public override void ExposeData()
	{
		Scribe_References.Look(ref mapParent, "mapParent");
		Scribe_Values.Look(ref cell, "cell");
		Scribe_Defs.Look(ref landingPodDef, "landingPodDef");
		Scribe_Defs.Look(ref activeTransporterDef, "activeTransporterDef");
	}
}
