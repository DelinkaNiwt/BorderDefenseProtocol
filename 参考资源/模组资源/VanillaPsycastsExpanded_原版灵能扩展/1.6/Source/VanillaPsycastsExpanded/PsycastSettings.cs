using Verse;

namespace VanillaPsycastsExpanded;

public class PsycastSettings : ModSettings
{
	public float additionalAbilityChance = 0.1f;

	public float baseSpawnChance = 0.1f;

	public bool changeFocusGain;

	public int maxLevel = 30;

	public bool muteSkipdoor;

	public bool shrink = true;

	public MultiCheckboxState smallMode = MultiCheckboxState.Partial;

	public float XPPerPercent = 1f;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref XPPerPercent, "xpPerPercent", 1f);
		Scribe_Values.Look(ref baseSpawnChance, "baseSpawnChance", 0.1f);
		Scribe_Values.Look(ref additionalAbilityChance, "additionalAbilityChance", 0.1f);
		Scribe_Values.Look(ref shrink, "shrink", defaultValue: true);
		Scribe_Values.Look(ref muteSkipdoor, "muteSkipdoor", defaultValue: false);
		Scribe_Values.Look(ref smallMode, "smallMode", MultiCheckboxState.Partial);
		Scribe_Values.Look(ref maxLevel, "maxLevel", 30);
		Scribe_Values.Look(ref changeFocusGain, "changeFocusGain", defaultValue: false);
	}
}
