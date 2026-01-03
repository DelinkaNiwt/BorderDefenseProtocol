using System;
using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Milira;

public class IncidentWorker_Milian_SmallCluster_SingleTurret : IncidentWorker
{
	private int randomDormantSec = Rand.Range(80, 120);

	public PawnkindWithCommonality_Extension Props => ((Def)def).GetModExtension<PawnkindWithCommonality_Extension>();

	protected override bool CanFireNowSub(IncidentParms parms)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		if (faction == null)
		{
			return false;
		}
		if (!MiliraRaceSettings.MiliraRace_ModSetting_MilianSmallClusterInMap)
		{
			return false;
		}
		if (Faction.OfPlayer.def.defName == "Milira_PlayerFaction" || Faction.OfPlayer.def.categoryTag == "Kiiro_PlayerFaction")
		{
			return false;
		}
		if (Current.Game.GetComponent<MiliraGameComponent_OverallControl>().turnToFriend)
		{
			return false;
		}
		return faction.HostileTo(Faction.OfPlayer);
	}

	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		Map map = (Map)parms.target;
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		List<TargetInfo> list = new List<TargetInfo>();
		List<ThingDef> source = DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef def) => def.building != null && def.building.buildingTags != null && def.building.buildingTags.Contains("Milira_SmallCluster_SingleTurret")).ToList();
		ThingDef turretDef = source.RandomElementWithFallback();
		IntVec3 intVec = FindDropPodLocation(map, (IntVec3 spot) => CanPlaceAt(spot));
		if (intVec == IntVec3.Invalid)
		{
			return false;
		}
		List<Pawn> list2 = new List<Pawn>();
		float num = Mathf.Max(parms.points * 0.3f, 800f);
		float pointsLeft;
		PawnkindWithCommonality result;
		for (pointsLeft = num; pointsLeft > 0f && Props.pawnkindsWithCommonality.Where((PawnkindWithCommonality p) => p.pawnkindDef.combatPower <= pointsLeft).TryRandomElementByWeight((PawnkindWithCommonality p) => p.commonality, out result); pointsLeft -= result.pawnkindDef.combatPower)
		{
			list2.Add(PawnGenerator.GeneratePawn(new PawnGenerationRequest(result.pawnkindDef, faction, PawnGenerationContext.NonPlayer, -1)));
		}
		Thing thing = ThingMaker.MakeThing(turretDef);
		thing.SetFaction(faction);
		CompInitiatable compInitiatable = thing.TryGetComp<CompInitiatable>();
		if (compInitiatable != null)
		{
			compInitiatable.initiationDelayTicksOverride = (int)(60f * (float)randomDormantSec);
		}
		List<Thing> list3 = new List<Thing>();
		list3.Add(thing);
		if (parms.points > 6000f)
		{
			List<ThingDef> list4 = DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef def) => def.building != null && def.building.buildingTags != null && def.building.buildingTags.Contains("MiliraClusterMemberResonator")).ToList();
			int num2 = Mathf.Min((int)(2f + parms.points / 1000f), 5);
			if (!list4.NullOrEmpty())
			{
				for (int num3 = 0; num3 < num2; num3++)
				{
					Thing thing2 = ThingMaker.MakeThing(list4.RandomElementWithFallback());
					thing2.SetFaction(faction);
					IntVec3 loc = (from v in GenRadial.RadialCellsAround(intVec, 4f, useCenter: false)
						where CanPlaceAt(v)
						select v).RandomElementWithFallback();
					if (loc.IsValid)
					{
						list3.Add(thing2);
						GenSpawn.Spawn(SkyfallerMaker.MakeSkyfaller(MiliraDefOf.Milira_CorePartIncoming, thing2), loc, map);
					}
				}
			}
		}
		int num4 = Mathf.Min((int)(parms.points / 2000f), 3);
		for (int num5 = 0; num5 < num4; num5++)
		{
			Thing thing3 = ThingMaker.MakeThing(MiliraDefOf.Milira_ProjectionNode);
			thing3.SetFaction(faction);
			IntVec3 loc2 = (from v in GenRadial.RadialCellsAround(intVec, 10f, useCenter: false)
				where CanPlaceAt(v)
				select v).RandomElementWithFallback();
			if (loc2.IsValid)
			{
				GenSpawn.Spawn(SkyfallerMaker.MakeSkyfaller(MiliraDefOf.Milira_CorePartIncoming, thing3), loc2, map);
			}
		}
		LordMaker.MakeNewLord(faction, new LordJob_SleepThenMechanoidsDefend(list3, faction, 28f, intVec, canAssaultColony: true, isMechCluster: false), map, list2);
		DropPodUtility.DropThingsNear(intVec, map, list2.Cast<Thing>(), 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, faction);
		foreach (Pawn item in list2)
		{
			item.TryGetComp<CompCanBeDormant>()?.ToSleep();
		}
		list.AddRange(list2.Select((Pawn p) => new TargetInfo(p)));
		GenSpawn.Spawn(SkyfallerMaker.MakeSkyfaller(MiliraDefOf.Milira_CorePartIncoming, thing), intVec, map);
		list.Add(new TargetInfo(intVec, map));
		SendStandardLetter(parms, list);
		return true;
		bool CanPlaceAt(IntVec3 intVec2)
		{
			CellRect cellRect = GenAdj.OccupiedRect(intVec2, Rot4.North, turretDef.Size);
			if (intVec2.Fogged(map) || !cellRect.InBounds(map))
			{
				return false;
			}
			if (!DropCellFinder.SkyfallerCanLandAt(intVec2, map, turretDef.Size))
			{
				return false;
			}
			foreach (IntVec3 item2 in cellRect)
			{
				RoofDef roof = item2.GetRoof(map);
				if (roof != null && roof.isNatural)
				{
					return false;
				}
			}
			return GenConstruct.CanBuildOnTerrain(turretDef, intVec2, map, Rot4.North);
		}
	}

	private static IntVec3 FindDropPodLocation(Map map, Predicate<IntVec3> validator)
	{
		for (int i = 0; i < 200; i++)
		{
			IntVec3 originCell = RCellFinder.FindSiegePositionFrom(DropCellFinder.FindRaidDropCenterDistant(map), map);
			RCellFinder.TryFindRandomSpotJustOutsideColony(originCell, map, out var result);
			if (validator(result))
			{
				return result;
			}
		}
		return IntVec3.Invalid;
	}
}
