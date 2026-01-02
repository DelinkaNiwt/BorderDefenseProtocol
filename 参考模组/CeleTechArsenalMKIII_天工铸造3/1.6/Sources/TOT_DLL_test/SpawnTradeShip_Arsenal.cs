using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class SpawnTradeShip_Arsenal
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
		Thing thing = ThingMaker.MakeThing(CMC_Def.CMC_TraderShuttle_A);
		TradeShip tradeShip = new TradeShip(DefDatabase<TraderKindDef>.GetNamed("CMC_OrbitalTrader_Weapons"));
		tradeShip.name = "CMC_TradeShipName_A".Translate();
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

	public void LandShip(Map map, Thing ship)
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
}
