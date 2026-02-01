using Verse;

namespace NCL;

internal class ModSettingsAbout : ModSettings
{
	public static bool GG_Disable_Settings_Window;

	public static StoryMode storyMode;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref GG_Disable_Settings_Window, "NCL_Disable_Settings_Window", defaultValue: false);
	}
}
