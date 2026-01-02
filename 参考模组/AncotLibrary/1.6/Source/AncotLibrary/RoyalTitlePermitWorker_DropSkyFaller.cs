using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class RoyalTitlePermitWorker_DropSkyFaller : RoyalTitlePermitWorker_Targeted
{
	private Faction faction;

	public override void OrderForceTarget(LocalTargetInfo target)
	{
		CallSkyFaller(target.Cell);
	}

	public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
	{
		if (faction.HostileTo(Faction.OfPlayer))
		{
			yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null);
			yield break;
		}
		Action action = null;
		string description = def.LabelCap + ": ";
		if (FillAidOption(pawn, faction, ref description, out var free))
		{
			action = delegate
			{
				BeginCallSkyFaller(pawn, faction, map, free);
			};
		}
		yield return new FloatMenuOption(description, action, faction.def.FactionIcon, faction.Color);
	}

	private void BeginCallSkyFaller(Pawn caller, Faction faction, Map map, bool free)
	{
		targetingParameters = new TargetingParameters();
		targetingParameters.canTargetLocations = true;
		targetingParameters.canTargetBuildings = false;
		targetingParameters.canTargetPawns = false;
		base.caller = caller;
		base.map = map;
		this.faction = faction;
		base.free = free;
		targetingParameters.validator = delegate(TargetInfo target)
		{
			if (def.royalAid.targetingRange > 0f && target.Cell.DistanceTo(caller.Position) > def.royalAid.targetingRange)
			{
				return false;
			}
			return target.Cell.Walkable(map) && !target.Cell.Fogged(map);
		};
		Find.Targeter.BeginTargeting(this);
	}

	private void CallSkyFaller(IntVec3 cell)
	{
		List<ThingDef> list = new List<ThingDef>();
		for (int i = 0; i < def.royalAid.itemsToDrop.Count; i++)
		{
			for (int j = 0; j < def.royalAid.itemsToDrop[i].count; j++)
			{
				SkyfallerMaker.SpawnSkyfaller(def.royalAid.itemsToDrop[i].thingDef, GenRadial.RadialCellsAround(cell, def.royalAid.radius, useCenter: true).RandomElementWithFallback(), map);
			}
			list.Add(def.royalAid.itemsToDrop[i].thingDef);
		}
		if (list.Any())
		{
			Messages.Message("Ancot.MessagePermitPerformed".Translate(faction.Named("FACTION")), new LookTargets(cell, map), MessageTypeDefOf.NeutralEvent);
			caller.royalty.GetPermit(def, faction).Notify_Used();
			if (!free)
			{
				caller.royalty.TryRemoveFavor(faction, def.royalAid.favorCost);
			}
		}
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		GenDraw.DrawRadiusRing(target.Cell, def.royalAid.radius + def.royalAid.explosionRadiusRange.max, Color.white);
		if (target.IsValid)
		{
			GenDraw.DrawTargetHighlight(target);
		}
	}
}
