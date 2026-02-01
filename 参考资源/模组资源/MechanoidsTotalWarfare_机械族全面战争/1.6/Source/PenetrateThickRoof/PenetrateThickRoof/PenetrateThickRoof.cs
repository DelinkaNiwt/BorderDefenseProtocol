using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace PenetrateThickRoof;

[StaticConstructorOnStartup]
public static class PenetrateThickRoof
{
	public static readonly Harmony harmony;

	public static readonly MethodInfo original;

	public static readonly HarmonyMethod transpiler;

	static PenetrateThickRoof()
	{
		harmony = new Harmony("AmCh.PenetrateThickRoof");
		original = typeof(Projectile).GetMethod("ImpactSomething", BindingFlags.Instance | BindingFlags.NonPublic);
		transpiler = new HarmonyMethod(typeof(PenetrateThickRoof).GetMethod("Transpiler"));
		harmony.Patch(original, null, null, transpiler);
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> source = instructions.ToList();
		List<CodeInstruction> result = new List<CodeInstruction>(source.Count + 3);
		FieldInfo f_isThickRoof = typeof(RoofDef).GetField("isThickRoof");
		MethodInfo m_ShouldBypassThickRoof = typeof(PenetrateThickRoof).GetMethod("ShouldBypassThickRoof");
		bool patched = false;
		for (int i = 0; i < source.Count; i++)
		{
			result.Add(source[i]);
			if (!patched && i >= 3 && source[i - 3].opcode == OpCodes.Brfalse && source[i - 1].LoadsField(f_isThickRoof) && source[i].opcode == OpCodes.Brfalse_S)
			{
				patched = true;
				result.Add(new CodeInstruction(OpCodes.Ldarg_0));
				result.Add(new CodeInstruction(OpCodes.Call, m_ShouldBypassThickRoof));
				result.Add(new CodeInstruction(OpCodes.Brtrue, source[i - 3].operand));
			}
		}
		return result;
	}

	public static bool ShouldBypassThickRoof(Thing thing)
	{
		return thing.def.HasModExtension<PenetrateThickRoofExtension>();
	}
}
