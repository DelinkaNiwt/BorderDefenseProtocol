using HarmonyLib;
using Verse;

namespace ATFieldGenerator;

[StaticConstructorOnStartup]
public static class Patch_AbsoluteTerrorField
{
	static Patch_AbsoluteTerrorField()
	{
		Harmony harmony = new Harmony("ATFieldGenerator.Patches");
		harmony.PatchAll();
	}
}
