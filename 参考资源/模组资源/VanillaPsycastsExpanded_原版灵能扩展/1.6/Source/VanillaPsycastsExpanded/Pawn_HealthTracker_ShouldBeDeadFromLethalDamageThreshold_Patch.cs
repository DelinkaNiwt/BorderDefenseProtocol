using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDeadFromLethalDamageThreshold")]
public static class Pawn_HealthTracker_ShouldBeDeadFromLethalDamageThreshold_Patch
{
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		List<CodeInstruction> codes = instructions.ToList();
		Label label = generator.DefineLabel();
		for (int i = 0; i < codes.Count; i++)
		{
			yield return codes[i];
			if (codes[i].opcode == OpCodes.Brfalse_S && codes[i - 1].opcode == OpCodes.Isinst && codes[i - 1].OperandIs(typeof(Hediff_Injury)))
			{
				codes[i + 1].labels.Add(label);
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, typeof(Pawn_HealthTracker).Field("hediffSet"));
				yield return new CodeInstruction(OpCodes.Ldfld, typeof(HediffSet).Field("hediffs"));
				yield return new CodeInstruction(OpCodes.Ldloc_1);
				yield return new CodeInstruction(OpCodes.Callvirt, typeof(List<Hediff>).IndexerGetter(new Type[1] { typeof(int) }));
				yield return new CodeInstruction(OpCodes.Call, typeof(Pawn_HealthTracker_ShouldBeDeadFromLethalDamageThreshold_Patch).Method("IsNotRegeneratingHediff"));
				yield return new CodeInstruction(OpCodes.Brfalse_S, codes[i].operand);
			}
		}
	}

	public static bool IsNotRegeneratingHediff(Hediff hediff)
	{
		return hediff.def != VPE_DefOf.VPE_Regenerating;
	}
}
