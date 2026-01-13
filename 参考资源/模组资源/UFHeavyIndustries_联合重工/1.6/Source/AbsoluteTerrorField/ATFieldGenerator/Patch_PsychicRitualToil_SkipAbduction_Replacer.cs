using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch]
public static class Patch_PsychicRitualToil_SkipAbduction_Replacer
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		Type type = AccessTools.TypeByName("Verse.AI.Group.PsychicRitualToil_SkipAbduction");
		if (type != null)
		{
			yield return AccessTools.Method(type, "ApplyOutcome");
		}
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo newMethod = AccessTools.Method(typeof(ATFieldInterceptUtility), "TryRandomElementWithAbductionCheck");
		MethodInfo method = default(MethodInfo);
		foreach (CodeInstruction inst in instructions)
		{
			int num;
			if (inst.opcode == OpCodes.Call)
			{
				object operand = inst.operand;
				method = operand as MethodInfo;
				num = (((object)method != null) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			if (num != 0 && method.DeclaringType == typeof(GenCollection) && method.Name == "TryRandomElement")
			{
				ParameterInfo[] parameters = method.GetParameters();
				if (parameters.Length >= 2 && parameters[1].ParameterType.GetElementType() == typeof(Pawn))
				{
					yield return new CodeInstruction(OpCodes.Call, newMethod);
					continue;
				}
			}
			yield return inst;
			method = null;
		}
	}
}
