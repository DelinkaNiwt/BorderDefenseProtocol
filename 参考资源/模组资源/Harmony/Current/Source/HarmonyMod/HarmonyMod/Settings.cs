using LudeonTK;
using Verse;

namespace HarmonyMod;

public class Settings : ModSettings
{
	[TweakValue("Harmony", 0f, 100f)]
	public static bool noStacktraceCaching;

	[TweakValue("Harmony", 0f, 100f)]
	public static bool noStacktraceEnhancing;

	private static void noStacktraceCaching_Changed()
	{
		Main.settings.Write();
	}

	private static void noStacktraceEnhancing_Changed()
	{
		Main.settings.Write();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref noStacktraceCaching, "noStacktraceCaching", defaultValue: false);
		Scribe_Values.Look(ref noStacktraceEnhancing, "noStacktraceEnhancing", defaultValue: false);
	}
}
