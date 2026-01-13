using System.Reflection;
using HarmonyLib;
using Verse;

namespace ECT;

[StaticConstructorOnStartup]
public static class ECT_Harmony
{
	static ECT_Harmony()
	{
		Harmony harmony = new Harmony("ECT.ModPatch");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
