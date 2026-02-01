using HarmonyLib;
using Verse;

namespace MechanitorCommandRange;

[StaticConstructorOnStartup]
public static class Main
{
	static Main()
	{
		Harmony harmony = new Harmony("rimworld.Nyar.MechanoidTotalWarfare");
		harmony.PatchAll();
	}
}
