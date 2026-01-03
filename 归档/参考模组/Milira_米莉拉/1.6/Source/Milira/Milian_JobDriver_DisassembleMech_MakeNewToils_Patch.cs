using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

[HarmonyPatch(typeof(JobDriver_DisassembleMech), "MakeNewToils")]
public static class Milian_JobDriver_DisassembleMech_MakeNewToils_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(JobDriver_DisassembleMech __instance, ref IEnumerable<Toil> __result)
	{
		Pawn value = Traverse.Create(__instance).Property<Pawn>("Mech").Value;
		if (MilianUtility.IsMilian(value) && !ModsConfig.IsActive("Ancot.MilianModification"))
		{
			__result = JobDriver_DisassembleMech_Helpers.DisassembleMilianToils(value, __instance.pawn);
			return false;
		}
		return true;
	}
}
