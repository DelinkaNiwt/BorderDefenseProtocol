using Verse;

namespace AlienRace;

public class AlienRaceSettings : ModSettings
{
	public bool textureLogs;

	public bool randomizeStartingPawnsOnReroll = true;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref textureLogs, "textureLogs", defaultValue: false);
		Scribe_Values.Look(ref randomizeStartingPawnsOnReroll, "randomizeStartingPawnsOnReroll", defaultValue: true);
	}

	public void UpdateSettings()
	{
	}
}
