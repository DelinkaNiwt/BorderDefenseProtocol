using System.Reflection;
using HarmonyLib;
using Verse;

namespace HNGT;

[StaticConstructorOnStartup]
public static class HNGT_Harmony
{
	static HNGT_Harmony()
	{
		Harmony harmony = new Harmony("HNGT.HeavyNavalGunTurret");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
