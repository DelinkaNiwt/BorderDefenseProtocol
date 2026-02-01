using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class TotalWarfareMod : Mod
{
	private TotalWarfareSettings settings;

	public static TotalWarfareMod Instance { get; private set; }

	public TotalWarfareSettings Settings { get; private set; }

	public TotalWarfareMod(ModContentPack content)
		: base(content)
	{
		settings = GetSettings<TotalWarfareSettings>();
		Instance = this;
	}

	public override string SettingsCategory()
	{
		return "NCL_TOTALWARFARE_SETTINGS_CATEGORY".Translate();
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		Listing_Standard listing = new Listing_Standard();
		listing.Begin(inRect);
		Rect basicLabelRect = listing.GetRect(Text.LineHeight);
		Widgets.Label(basicLabelRect, "NCL_BASIC_SETTINGS".Translate());
		if (!string.IsNullOrEmpty("NCL_BASIC_SETTINGS_DESC".Translate()))
		{
			TooltipHandler.TipRegion(basicLabelRect, "NCL_BASIC_SETTINGS_DESC".Translate());
		}
		listing.Gap();
		Rect beaconRect = listing.GetRect(Text.LineHeight);
		Widgets.CheckboxLabeled(beaconRect, "NCL_TOTALWARFARE_ENABLE_BEACON_ENHANCEMENT".Translate(), ref TotalWarfareSettings.EnableMechEnhancement);
		if (!string.IsNullOrEmpty("NCL_TOTALWARFARE_ENABLE_BEACON_ENHANCEMENT_DESC".Translate()))
		{
			TooltipHandler.TipRegion(beaconRect, "NCL_TOTALWARFARE_ENABLE_BEACON_ENHANCEMENT_DESC".Translate());
		}
		listing.Gap();
		listing.Label("NCL_AIRSTRIKE_TRIGGER_SETTINGS".Translate());
		listing.Gap(8f);
		Rect airstrikeRect = listing.GetRect(30f);
		Widgets.Label(airstrikeRect.LeftHalf(), "NCL_AIRSTRIKE_THRESHOLD".Translate());
		string airstrikeBuffer = TotalWarfareSettings.AirstrikeWealthThreshold.ToString("F0");
		Widgets.TextFieldNumeric(airstrikeRect.RightHalf(), ref TotalWarfareSettings.AirstrikeWealthThreshold, ref airstrikeBuffer, 0f, 10000000f);
		listing.Gap();
		listing.Label("NCL_WEALTH_TRIGGER_SETTINGS".Translate());
		Rect wealthRect = listing.GetRect(Text.LineHeight);
		Widgets.CheckboxLabeled(wealthRect, "NCL_TOTALWARFARE_ENABLE_WEALTH_TRIGGER".Translate(), ref TotalWarfareSettings.EnableAutoTrigger);
		if (!string.IsNullOrEmpty("NCL_TOTALWARFARE_ENABLE_WEALTH_TRIGGER_DESC".Translate()))
		{
			TooltipHandler.TipRegion(wealthRect, "NCL_TOTALWARFARE_ENABLE_WEALTH_TRIGGER_DESC".Translate());
		}
		listing.Gap(8f);
		Rect thresholdRect = listing.GetRect(30f);
		Widgets.Label(thresholdRect.LeftHalf(), "NCL_WEALTH_THRESHOLD".Translate());
		string thresholdBuffer = TotalWarfareSettings.WealthTriggerThreshold.ToString("F0");
		Widgets.TextFieldNumeric(thresholdRect.RightHalf(), ref TotalWarfareSettings.WealthTriggerThreshold, ref thresholdBuffer, 0f, 10000000f);
		listing.Gap();
		Rect coreRect = listing.GetRect(Text.LineHeight);
		Widgets.CheckboxLabeled(coreRect, "NCL_ENABLE_CORETRIGGER".Translate(), ref TotalWarfareSettings.EnableCoreTrigger);
		if (!string.IsNullOrEmpty("NCL_ENABLE_CORETRIGGER_DESC".Translate()))
		{
			TooltipHandler.TipRegion(coreRect, "NCL_ENABLE_CORETRIGGER_DESC".Translate());
		}
		listing.Gap();
		try
		{
			Rect dailyLimitRect = listing.GetRect(Text.LineHeight);
			Widgets.Label(dailyLimitRect, "NCL_DAILY_SPECIAL_HEDIFF_LIMIT".Translate());
			if (!string.IsNullOrEmpty("NCL_DAILY_SPECIAL_HEDIFF_LIMIT_DESC".Translate()))
			{
				TooltipHandler.TipRegion(dailyLimitRect, "NCL_DAILY_SPECIAL_HEDIFF_LIMIT_DESC".Translate());
			}
			listing.Gap();
			Rect numberEntryRect = listing.GetRect(30f);
			if (TotalWarfareSettings.MaxSpecialHediffsPerDay <= 0)
			{
				TotalWarfareSettings.MaxSpecialHediffsPerDay = 50;
			}
			int currentValue = TotalWarfareSettings.MaxSpecialHediffsPerDay;
			string buffer = currentValue.ToString();
			Widgets.TextFieldNumeric(numberEntryRect, ref currentValue, ref buffer, 1f, 1000f);
			TotalWarfareSettings.MaxSpecialHediffsPerDay = currentValue;
		}
		catch (Exception arg)
		{
			Log.Error($"Error in daily counter UI: {arg}");
		}
		listing.Gap(20f);
		try
		{
			Rect dailyLimitRect2 = listing.GetRect(Text.LineHeight);
			Widgets.Label(dailyLimitRect2, "NCL_DAILY_SPECIAL_HEDIFF_LIMIT_A".Translate());
			if (!string.IsNullOrEmpty("NCL_DAILY_SPECIAL_HEDIFF_LIMIT_DESC_A".Translate()))
			{
				TooltipHandler.TipRegion(dailyLimitRect2, "NCL_DAILY_SPECIAL_HEDIFF_LIMIT_DESC_A".Translate());
			}
			listing.Gap();
			Rect numberEntryRect2 = listing.GetRect(30f);
			if (TotalWarfareSettings.MaxSpecialHediffsPerDayA <= 0)
			{
				TotalWarfareSettings.MaxSpecialHediffsPerDayA = 50;
			}
			int currentValue2 = TotalWarfareSettings.MaxSpecialHediffsPerDayA;
			string buffer2 = currentValue2.ToString();
			Widgets.TextFieldNumeric(numberEntryRect2, ref currentValue2, ref buffer2, 1f, 1000f);
			TotalWarfareSettings.MaxSpecialHediffsPerDayA = currentValue2;
		}
		catch (Exception arg2)
		{
			Log.Error($"Error in daily counter UI: {arg2}");
		}
		listing.Gap(20f);
		Rect operationsLabelRect = listing.GetRect(Text.LineHeight);
		Widgets.Label(operationsLabelRect, "NCL_OPERATIONS".Translate());
		if (!string.IsNullOrEmpty("NCL_OPERATIONS_DESC".Translate()))
		{
			TooltipHandler.TipRegion(operationsLabelRect, "NCL_OPERATIONS_DESC".Translate());
		}
		listing.Gap();
		Rect buttonRect = listing.GetRect(30f);
		if (Widgets.ButtonText(buttonRect, "NCL_GENERATE_ANCIENT_WAR_BEACON_REMAINS".Translate()))
		{
			ExecuteAncientWarBeaconRemains();
		}
		if (!string.IsNullOrEmpty("NCL_GENERATE_ANCIENT_WAR_BEACON_REMAINS_DESC".Translate()))
		{
			TooltipHandler.TipRegion(buttonRect, "NCL_GENERATE_ANCIENT_WAR_BEACON_REMAINS_DESC".Translate());
		}
		listing.End();
	}

	private void ExecuteAncientWarBeaconRemains()
	{
		Map targetMap = GetPlayerCurrentMap();
		if (targetMap == null)
		{
			Messages.Message("NCL_NO_CURRENT_MAP_FOUND".Translate(), MessageTypeDefOf.RejectInput);
			return;
		}
		try
		{
			if (targetMap.wasSpawnedViaGravShipLanding && (targetMap.mapDrawer == null || targetMap.terrainGrid == null))
			{
				Messages.Message("NCL_MAP_NOT_READY_FOR_GENERATION".Translate(), MessageTypeDefOf.RejectInput);
				return;
			}
			IntVec3 centerPos = FindSuitablePosition(targetMap);
			if (!centerPos.IsValid)
			{
				Messages.Message("NCL_NO_VALID_SPOT_FOUND".Translate(), MessageTypeDefOf.RejectInput);
				return;
			}
			GenerateExostriderRemains(targetMap, centerPos);
			FleckMaker.ThrowDustPuffThick(centerPos.ToVector3Shifted(), targetMap, 8f, Color.gray);
			Messages.Message("NCL_SUCCESS_GENERATE_ANCIENT_WAR_BEACON".Translate(centerPos.x, centerPos.z), MessageTypeDefOf.PositiveEvent);
		}
		catch (Exception ex)
		{
			Log.Error($"Error executing AncientWarBeaconRemains: {ex}");
			Messages.Message("NCL_FAILED_GENERATE".Translate(ex.Message), MessageTypeDefOf.ThreatBig);
		}
	}

	private IntVec3 FindSuitablePosition(Map map)
	{
		int minEdgeDist = 15;
		CellRect safeArea = new CellRect(minEdgeDist, minEdgeDist, map.Size.x - minEdgeDist * 2, map.Size.z - minEdgeDist * 2);
		for (int attempt = 0; attempt < 100; attempt++)
		{
			IntVec3 candidatePos = safeArea.RandomCell;
			if (candidatePos.InBounds(map) && candidatePos.Standable(map))
			{
				float minDist = (float)Mathf.Min(map.Size.x, map.Size.z) * 0.333f;
				if (!(candidatePos.DistanceTo(map.Center) < minDist) && candidatePos.GetEdifice(map) == null && !map.areaManager.Home[candidatePos] && !GenRadial.RadialDistinctThingsAround(candidatePos, map, 15f, useCenter: true).Any((Thing t) => t.Faction == Faction.OfPlayer && t.def.category == ThingCategory.Building))
				{
					return candidatePos;
				}
			}
		}
		return IntVec3.Invalid;
	}

	private void GenerateExostriderRemains(Map map, IntVec3 center)
	{
		ClearArea(map, center, 5);
		PlacePart(map, center, "AncientExostriderHead", new IntVec3(2, 0, 0));
		PlacePart(map, center, "AncientExostriderRemains", IntVec3.Zero);
		PlacePart(map, center, "Building_Ancient_WarBeacon", new IntVec3(-2, 0, -2));
		PlacePart(map, center, "AncientExostriderCannon", new IntVec3(-1, 0, 2));
		PlacePart(map, center, "AncientExostriderLeg", new IntVec3(-3, 0, 1));
		PlacePart(map, center, "AncientExostriderLeg", new IntVec3(-4, 0, 0));
		PlacePart(map, center, "AncientExostriderLeg", new IntVec3(1, 0, -2), Rot4.West);
		PlacePart(map, center, "Ancient_WarBeacon_PartsB", new IntVec3(0, 0, -3), Rot4.West);
		PlacePart(map, center, "Ancient_WarBeacon_PartsA", new IntVec3(-2, 0, -4));
		PlacePart(map, center, "Broken_War_Scorpion_Rust", new IntVec3(2, 0, 3));
		GenerateFilthAroundParts(map, center, 5);
	}

	private void ClearArea(Map map, IntVec3 center, int radius)
	{
		foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
		{
			if (!cell.InBounds(map) || !cell.Standable(map))
			{
				continue;
			}
			List<Thing> things = cell.GetThingList(map);
			for (int i = things.Count - 1; i >= 0; i--)
			{
				if (things[i].def.category == ThingCategory.Building || things[i].def.category == ThingCategory.Item)
				{
					things[i].Destroy();
				}
			}
			cell.GetPlant(map)?.Destroy();
		}
	}

	private void PlacePart(Map map, IntVec3 center, string thingDefName, IntVec3 offset, Rot4? rotation = null)
	{
		IntVec3 position = center + offset;
		if (!position.InBounds(map))
		{
			return;
		}
		ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
		if (thingDef == null)
		{
			Log.Warning("Missing thing def: " + thingDefName);
			return;
		}
		Thing part = ThingMaker.MakeThing(thingDef);
		part.SetFaction(null);
		if (rotation.HasValue)
		{
			part.Rotation = rotation.Value;
		}
		GenPlace.TryPlaceThing(part, position, map, ThingPlaceMode.Direct);
	}

	private void GenerateFilthAroundParts(Map map, IntVec3 center, int radius)
	{
		foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
		{
			if (!cell.InBounds(map) || !cell.Standable(map) || !cell.GetThingList(map).Any((Thing t) => t.def.defName.Contains("Ancient") || t.def.defName.Contains("Broken_War") || t.def.defName.Contains("Building_Ancient")))
			{
				continue;
			}
			foreach (IntVec3 filthCell in GenRadial.RadialCellsAround(cell, 1f, useCenter: true))
			{
				if (filthCell.InBounds(map) && filthCell.Standable(map) && Rand.Chance(0.6f))
				{
					FilthMaker.TryMakeFilth(filthCell, map, DefDatabase<ThingDef>.GetNamed("Filth_MachineBits"));
				}
			}
		}
	}

	private Map GetPlayerCurrentMap()
	{
		List<Pawn> freeColonists = new List<Pawn>();
		foreach (Map map in Find.Maps)
		{
			freeColonists.AddRange(map.mapPawns.FreeColonists);
		}
		List<Pawn> validColonists = freeColonists.Where((Pawn pawn) => pawn.Spawned && !pawn.Dead).ToList();
		if (validColonists.Count > 0)
		{
			Pawn randomColonist = validColonists.RandomElement();
			return randomColonist.Map;
		}
		return null;
	}
}
