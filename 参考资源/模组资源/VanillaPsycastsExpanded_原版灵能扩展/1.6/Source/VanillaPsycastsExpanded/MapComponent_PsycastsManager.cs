using System.Collections.Generic;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class MapComponent_PsycastsManager : MapComponent
{
	public List<FixedTemperatureZone> temperatureZones = new List<FixedTemperatureZone>();

	public List<Hediff_BlizzardSource> blizzardSources = new List<Hediff_BlizzardSource>();

	public List<Hediff_Overlay> hediffsToDraw = new List<Hediff_Overlay>();

	public MapComponent_PsycastsManager(Map map)
		: base(map)
	{
	}

	public override void MapComponentTick()
	{
		base.MapComponentTick();
		for (int num = temperatureZones.Count - 1; num >= 0; num--)
		{
			FixedTemperatureZone fixedTemperatureZone = temperatureZones[num];
			if (Find.TickManager.TicksGame >= fixedTemperatureZone.expiresIn)
			{
				temperatureZones.RemoveAt(num);
			}
			else
			{
				fixedTemperatureZone.DoEffects(map);
			}
		}
	}

	public bool TryGetOverridenTemperatureFor(IntVec3 cell, out float result)
	{
		foreach (FixedTemperatureZone temperatureZone in temperatureZones)
		{
			if (cell.DistanceTo(temperatureZone.center) <= temperatureZone.radius)
			{
				result = temperatureZone.fixedTemperature;
				return true;
			}
		}
		foreach (Hediff_BlizzardSource blizzardSource in blizzardSources)
		{
			if (cell.DistanceTo(((Hediff)(object)blizzardSource).pawn.Position) <= ((Hediff_Ability)blizzardSource).ability.GetRadiusForPawn())
			{
				result = -60f;
				return true;
			}
		}
		result = -1f;
		return false;
	}

	public override void MapComponentUpdate()
	{
		base.MapComponentUpdate();
		for (int num = hediffsToDraw.Count - 1; num >= 0; num--)
		{
			Hediff_Overlay hediff_Overlay = hediffsToDraw[num];
			if (((Hediff)(object)hediff_Overlay).pawn == null || !((Hediff)(object)hediff_Overlay).pawn.health.hediffSet.hediffs.Contains((Hediff)(object)hediff_Overlay))
			{
				hediffsToDraw.RemoveAt(num);
			}
			else if (((Hediff)(object)hediff_Overlay).pawn?.MapHeld != null)
			{
				hediff_Overlay.Draw();
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref temperatureZones, "temperatureZones", LookMode.Deep);
		Scribe_Collections.Look(ref blizzardSources, "blizzardSources", LookMode.Reference);
		Scribe_Collections.Look(ref hediffsToDraw, "hediffsToDraw", LookMode.Reference);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (temperatureZones == null)
			{
				temperatureZones = new List<FixedTemperatureZone>();
			}
			if (blizzardSources == null)
			{
				blizzardSources = new List<Hediff_BlizzardSource>();
			}
			if (hediffsToDraw == null)
			{
				hediffsToDraw = new List<Hediff_Overlay>();
			}
			temperatureZones.RemoveAll((FixedTemperatureZone x) => x == null);
			blizzardSources.RemoveAll((Hediff_BlizzardSource x) => x == null);
			hediffsToDraw.RemoveAll((Hediff_Overlay x) => x == null);
		}
	}
}
