using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class MainButtonWorker_Drones : MainButtonWorker_ToggleTab
{
	private const int CheckInterval = 252;

	private bool disabled;

	private int lastCheckTick = -1;

	private Map lastMap;

	private Pawn lastValidDrone;

	public override bool Disabled
	{
		get
		{
			if (base.Disabled)
			{
				return true;
			}
			Map currentMap = Find.CurrentMap;
			if (currentMap == null)
			{
				return true;
			}
			if (ShouldRecache(currentMap))
			{
				disabled = !HasDroneOnCurrentMap(currentMap);
			}
			return disabled;
		}
	}

	public override bool Visible => AncotLibrarySettings.drone_TabAvailable != TabAvailable.Button && !Disabled;

	private bool ShouldRecache(Map currentMap)
	{
		return currentMap != lastMap || Find.TickManager.TicksGame - lastCheckTick >= 252;
	}

	private bool HasDroneOnCurrentMap(Map currentMap)
	{
		lastMap = currentMap;
		lastCheckTick = Find.TickManager.TicksGame;
		if (lastValidDrone?.MapHeld == currentMap)
		{
			return true;
		}
		List<Pawn> spawnedColonyMechs = currentMap.mapPawns.SpawnedColonyMechs;
		for (int i = 0; i < spawnedColonyMechs.Count; i++)
		{
			if (spawnedColonyMechs[i].TryGetComp<CompDrone>() != null)
			{
				lastValidDrone = spawnedColonyMechs[i];
				return true;
			}
		}
		return false;
	}
}
