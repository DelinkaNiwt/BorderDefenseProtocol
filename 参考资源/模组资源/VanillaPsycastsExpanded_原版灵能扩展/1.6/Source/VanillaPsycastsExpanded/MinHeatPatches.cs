using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;

namespace VanillaPsycastsExpanded;

[HarmonyPatch]
public static class MinHeatPatches
{
	[HarmonyTargetMethods]
	public static IEnumerable<MethodInfo> TargetMethods()
	{
		Type type = typeof(Pawn_PsychicEntropyTracker);
		yield return AccessTools.Method(type, "TryAddEntropy");
		yield return AccessTools.Method(type, "PsychicEntropyTrackerTickInterval");
		yield return AccessTools.Method(type, "RemoveAllEntropy");
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		bool found = false;
		foreach (CodeInstruction instruction in instructions)
		{
			if (!found && instruction.opcode == OpCodes.Ldc_R4)
			{
				object operand = instruction.operand;
				if (operand is float && (float)operand == 0f)
				{
					found = true;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_PsychicEntropyTracker), "pawn"));
					yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VPE_DefOf), "VPE_PsychicEntropyMinimum"));
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StatExtension), "GetStatValue"));
					continue;
				}
			}
			yield return instruction;
		}
	}
}
