using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TOT_DLL_test;

public class Landed_CMCTS : TradeShip, ITrader
{
	private Map map;

	public int iniSilver = 0;

	public Landed_CMCTS()
	{
	}

	public Landed_CMCTS(Map map, TraderKindDef def, Faction faction = null)
		: base(def, faction)
	{
		this.map = map;
		passingShipManager = map.passingShipManager;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref map, "map");
		Scribe_Values.Look(ref iniSilver, "initsilver", 0);
	}

	public override void PassingShipTick()
	{
		base.PassingShipTick();
		if (passingShipManager == null)
		{
			passingShipManager = map.passingShipManager;
		}
	}

	public override void Depart()
	{
	}

	public new void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
	{
		Thing thing = toGive.SplitOff(countToGive);
		thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, this);
		if (!GenPlace.TryPlaceThing(thing, playerNegotiator.Position, base.Map, ThingPlaceMode.Near, null, null, default(Rot4)))
		{
			Log.Error(string.Concat("Could not place bought thing ", thing, " at ", playerNegotiator.Position));
			thing.Destroy();
		}
	}

	public bool ReachableForTrade(Pawn pawn, Thing thing)
	{
		return pawn.Map == thing.Map && pawn.Map.reachability.CanReach(pawn.Position, thing, PathEndMode.Touch, TraverseMode.PassDoors, Danger.Some);
	}

	public new IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
	{
		foreach (Thing item in TradeUtility.AllLaunchableThingsForTrade(base.Map, this))
		{
			yield return item;
		}
		foreach (Pawn item2 in TradeUtility.AllSellableColonyPawns(base.Map, checkAcceptableTemperatureOfAnimals: false))
		{
			yield return item2;
		}
	}
}
