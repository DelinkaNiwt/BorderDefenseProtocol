using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class SpawnTradeShip_Slave
{
	public bool SpawnShip()
	{
		Map currentMap = Find.CurrentMap;
		Map anyPlayerHomeMap = Find.AnyPlayerHomeMap;
		Thing thing = MakeTraderShip(currentMap);
		if (thing != null)
		{
			LandShip(currentMap, thing);
		}
		return true;
	}

	private static Thing MakeTraderShip(Map map)
	{
		Thing thing = ThingMaker.MakeThing(CMC_Def.CMC_TraderShuttle_S);
		TradeShip tradeShip = new TradeShip(DefDatabase<TraderKindDef>.GetNamed("CMC_OrbitalTrader_Slaves"));
		tradeShip.name = "CMC_TradeShipName_S".Translate();
		if (tradeShip == null)
		{
			throw new InvalidOperationException();
		}
		Comp_TraderShuttle comp_TraderShuttle = thing.TryGetComp<Comp_TraderShuttle>();
		comp_TraderShuttle.GenerateInternalTradeShip(map, tradeShip.def);
		return thing;
	}

	private bool UsableLZ(Building buildingTT, out Thing blocker)
	{
		blocker = null;
		foreach (IntVec3 item in buildingTT.OccupiedRect())
		{
			foreach (Thing thing in item.GetThingList(Find.CurrentMap))
			{
				if (!(thing is Pawn) && thing.def.Fillage != FillCategory.None)
				{
					blocker = thing;
					break;
				}
			}
			if (blocker != null)
			{
				break;
			}
		}
		if (blocker == null)
		{
			return true;
		}
		return false;
	}

	public virtual void LandShip(Map map, Thing ship)
	{
		IntVec3 center = IntVec3.Invalid;
		Comp_TraderShuttle comp_TraderShuttle = ship.TryGetComp<Comp_TraderShuttle>();
		List<Thing> list = map.listerThings.ThingsOfDef(CMC_Def.CMC_LandPlatform);
		Building_LandingPlatform building_LandingPlatform = null;
		if (!center.IsValid && list != null)
		{
			foreach (Thing item in list)
			{
				Thing blocker = null;
				CompPowerTrader compPowerTrader = item.TryGetComp<CompPowerTrader>();
				if (compPowerTrader != null && compPowerTrader.PowerOn && UsableLZ(item as Building, out blocker))
				{
					center = item.Position;
					building_LandingPlatform = item as Building_LandingPlatform;
				}
			}
		}
		if (center.IsValid)
		{
			Messages.Message("Message_CMC_TraderLanded".Translate(), ship, MessageTypeDefOf.PositiveEvent);
			if (building_LandingPlatform != null)
			{
				building_LandingPlatform.landingTick = 350;
			}
			GenPlace.TryPlaceThing(SkyfallerMaker.MakeSkyfaller(comp_TraderShuttle.Props.landAnimation, ship), center, map, ThingPlaceMode.Near, null, null, default(Rot4));
		}
		else
		{
			Messages.Message("Message_CMC_TraderCantLanded".Translate(), ship, MessageTypeDefOf.NeutralEvent);
		}
	}

	public static bool FindAnyLandingSpot(out IntVec3 spot, Faction faction, Map map, IntVec2? size)
	{
		if (!DropCellFinder.FindSafeLandingSpot(out spot, faction, map, 0, 15, 25, size))
		{
			IntVec3 intVec = DropCellFinder.RandomDropSpot(map);
			if (!DropCellFinder.TryFindDropSpotNear(intVec, map, out spot, allowFogged: false, canRoofPunch: false, allowIndoors: false, size))
			{
				spot = intVec;
			}
		}
		return true;
	}

	public static void FindCloseLandingSpot(out IntVec3 spot, Faction faction, Map map, IntVec2? size)
	{
		IntVec3 intVec = default(IntVec3);
		int num = 0;
		foreach (Building item in map.listerBuildings.allBuildingsColonist.Where((Building x) => x.def.size.x > 1 || x.def.size.z > 1))
		{
			intVec += item.Position;
			num++;
		}
		if (num == 0)
		{
			FindAnyLandingSpot(out spot, faction, map, size);
			return;
		}
		intVec.x /= num;
		intVec.z /= num;
		int num2 = 20;
		float num3 = 999999f;
		spot = default(IntVec3);
		for (int num4 = 0; num4 < num2; num4++)
		{
			FindAnyLandingSpot(out var spot2, faction, map, size);
			if ((float)(spot2 - intVec).LengthManhattan < num3)
			{
				num3 = (spot2 - intVec).LengthManhattan;
				spot = spot2;
			}
		}
	}
}
