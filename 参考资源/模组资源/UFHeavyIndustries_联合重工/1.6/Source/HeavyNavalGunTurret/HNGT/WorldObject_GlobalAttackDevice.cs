using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace HNGT;

public class WorldObject_GlobalAttackDevice : WorldObject
{
	public int startTile;

	public int destinationTile;

	public IntVec3 destinationCell;

	public Thing instigator;

	private float traveledPct;

	private bool arrived;

	public string payloadThingDefName;

	private float traveledPctStepPerTick_Cached = -1f;

	private DefModExtension_GlobalAttackDeviceParams ExtProps => def.GetModExtension<DefModExtension_GlobalAttackDeviceParams>();

	public override Vector3 DrawPos
	{
		get
		{
			Vector3 tileCenter = Find.WorldGrid.GetTileCenter(startTile);
			Vector3 tileCenter2 = Find.WorldGrid.GetTileCenter(destinationTile);
			return Vector3.Slerp(tileCenter, tileCenter2, traveledPct);
		}
	}

	private float TraveledPctStepPerTick
	{
		get
		{
			if (traveledPctStepPerTick_Cached >= 0f)
			{
				return traveledPctStepPerTick_Cached;
			}
			Vector3 tileCenter = Find.WorldGrid.GetTileCenter(startTile);
			Vector3 tileCenter2 = Find.WorldGrid.GetTileCenter(destinationTile);
			if (tileCenter == tileCenter2)
			{
				traveledPctStepPerTick_Cached = 1f;
				return traveledPctStepPerTick_Cached;
			}
			float num = GenMath.SphericalDistance(tileCenter.normalized, tileCenter2.normalized);
			if (num == 0f)
			{
				traveledPctStepPerTick_Cached = 1f;
				return traveledPctStepPerTick_Cached;
			}
			float num2 = 0.005f;
			if (ExtProps != null)
			{
				num2 = ExtProps.flightSpeed;
			}
			traveledPctStepPerTick_Cached = num2 / num;
			return traveledPctStepPerTick_Cached;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref startTile, "startTile", 0);
		Scribe_Values.Look(ref destinationTile, "destinationTile", 0);
		Scribe_Values.Look(ref destinationCell, "destinationCell");
		Scribe_References.Look(ref instigator, "instigator");
		Scribe_Values.Look(ref traveledPct, "traveledPct", 0f);
		Scribe_Values.Look(ref arrived, "arrived", defaultValue: false);
		Scribe_Values.Look(ref payloadThingDefName, "payloadThingDefName");
	}

	protected override void Tick()
	{
		base.Tick();
		traveledPct += TraveledPctStepPerTick;
		if (traveledPct >= 1f)
		{
			traveledPct = 1f;
			Arrived();
		}
	}

	private void Arrived()
	{
		if (arrived)
		{
			return;
		}
		arrived = true;
		Map map = Find.Maps.Find((Map m) => m.Tile == destinationTile);
		if (map == null)
		{
			Find.WorldObjects.Remove(this);
			return;
		}
		if (string.IsNullOrEmpty(payloadThingDefName))
		{
			Log.Error("HNGT: " + def.defName + " arrived but 'payloadThingDefName' is null.");
			Find.WorldObjects.Remove(this);
			return;
		}
		ThingDef named = DefDatabase<ThingDef>.GetNamed(payloadThingDefName, errorOnFail: false);
		if (named == null)
		{
			Log.Error("HNGT: " + def.defName + " can not find '" + payloadThingDefName + "' payloadThingDefName.");
			Find.WorldObjects.Remove(this);
		}
		else
		{
			OrbitalStrike orbitalStrike = (OrbitalStrike)GenSpawn.Spawn(named, destinationCell, map);
			orbitalStrike.instigator = instigator;
			Messages.Message("HNGT_GlobalAttackArrived".Translate(orbitalStrike.LabelCap, map.info.parent.Label), MessageTypeDefOf.PositiveEvent);
			Find.WorldObjects.Remove(this);
		}
	}
}
