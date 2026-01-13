using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(PowerNet), "PowerNetTick")]
public static class Patch_PowerNet_PowerNetTick
{
	public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
	{
		return instructions.MethodReplacer(AccessTools.Method(typeof(GameConditionManager), "ElectricityDisabled"), AccessTools.Method(typeof(Patch_PowerNet_PowerNetTick), "ElectricityDisabled"));
	}

	public static bool ElectricityDisabled(this GameConditionManager instance, Map map)
	{
		if (!instance.ElectricityDisabled(map))
		{
			return false;
		}
		if (ATFieldManager.Get(map).HasActiveSolarFlareShield())
		{
			return false;
		}
		return true;
	}
}
