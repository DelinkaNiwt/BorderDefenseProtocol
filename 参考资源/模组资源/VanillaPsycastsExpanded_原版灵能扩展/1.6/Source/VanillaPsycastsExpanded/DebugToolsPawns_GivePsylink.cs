using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(DebugToolsPawns), "GivePsylink")]
public static class DebugToolsPawns_GivePsylink
{
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo info1 = AccessTools.Field(typeof(HediffDefOf), "PsychicAmplifier");
		FieldInfo info2 = AccessTools.Field(typeof(HediffDef), "maxSeverity");
		foreach (CodeInstruction instruction in instructions)
		{
			if (!instruction.LoadsField(info1))
			{
				if (instruction.LoadsField(info2))
				{
					yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PsycasterPathDef), "TotalPoints"));
				}
				else
				{
					yield return instruction;
				}
			}
		}
	}
}
