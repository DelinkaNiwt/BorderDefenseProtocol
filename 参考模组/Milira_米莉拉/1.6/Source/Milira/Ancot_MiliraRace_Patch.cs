using System.Reflection;
using HarmonyLib;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class Ancot_MiliraRace_Patch
{
	static Ancot_MiliraRace_Patch()
	{
		Harmony harmony = new Harmony("Ancot.MiliraRaceHarmonyPatch");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
