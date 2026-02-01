using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch]
public static class Pawn_PsychicEntropyTracker_OptimizeGetters_Patch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return typeof(Pawn_PsychicEntropyTracker).DeclaredPropertyGetter("MaxEntropy");
		yield return typeof(Pawn_PsychicEntropyTracker).DeclaredPropertyGetter("MaxPotentialEntropy");
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, MethodBase baseMethod)
	{
		CodeMatcher codeMatcher = new CodeMatcher(instr);
		codeMatcher.MatchEndForward(CodeMatch.LoadsField(typeof(StatDefOf).DeclaredField("PsychicEntropyMax")), CodeMatch.LoadsConstant(1L), CodeMatch.LoadsConstant(-1L), CodeMatch.Calls((Expression<Func<_003C_003Ef__AnonymousDelegate0<Thing, StatDef, bool, int, float>>>)(() => StatExtension.GetStatValue)));
		if (codeMatcher.IsInvalid)
		{
			Log.Error("Patch to optimize " + baseMethod.DeclaringType?.Name + "." + baseMethod.Name + " failed, could not find code sequence responsible for accessing max psychic heat. Either vanilla code changed (was fixed?), or another mod modified this code.");
			return codeMatcher.Instructions();
		}
		codeMatcher.Advance(-1);
		codeMatcher.Opcode = OpCodes.Ldc_I4_S;
		codeMatcher.Operand = 60;
		return codeMatcher.Instructions();
	}
}
