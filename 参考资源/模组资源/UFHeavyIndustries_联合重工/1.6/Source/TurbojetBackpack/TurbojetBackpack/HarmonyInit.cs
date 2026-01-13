using HarmonyLib;
using Verse;

namespace TurbojetBackpack;

[StaticConstructorOnStartup]
public static class HarmonyInit
{
	static HarmonyInit()
	{
		Harmony harmony = new Harmony("com.turbojet.backpack");
		harmony.PatchAll();
	}
}
