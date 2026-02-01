using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace TowerLaserDefense.HarmonyPatches;

[UsedImplicitly]
[StaticConstructorOnStartup]
public class PatchMain
{
	static PatchMain()
	{
		Harmony harmony = new Harmony("TowerLaserDefense_HarmonyPatch");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
