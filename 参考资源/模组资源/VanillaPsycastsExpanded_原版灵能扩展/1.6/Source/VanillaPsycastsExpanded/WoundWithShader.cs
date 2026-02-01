using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch]
public class WoundWithShader : FleshTypeDef.Wound
{
	public ShaderTypeDef shader;

	[HarmonyPatch(typeof(FleshTypeDef.Wound), "Resolve")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Resolve_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.opcode == OpCodes.Stloc_0)
			{
				Label label = generator.DefineLabel();
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Isinst, typeof(WoundWithShader));
				yield return new CodeInstruction(OpCodes.Dup);
				yield return new CodeInstruction(OpCodes.Brfalse, label);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(WoundWithShader), "shader"));
				yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ShaderTypeDef), "Shader"));
				yield return new CodeInstruction(OpCodes.Stloc_0);
				yield return new CodeInstruction(OpCodes.Pop).WithLabels(label);
			}
		}
	}
}
