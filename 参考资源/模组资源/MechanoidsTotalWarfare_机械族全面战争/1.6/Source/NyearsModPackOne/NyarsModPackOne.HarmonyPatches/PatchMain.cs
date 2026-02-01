using System.Reflection;
using HarmonyLib;
using Verse;

namespace NyarsModPackOne.HarmonyPatches;

[StaticConstructorOnStartup]
public class PatchMain
{
	static PatchMain()
	{
		Harmony harmony = new Harmony("NyarsModPackOne_HarmonyPatch");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
