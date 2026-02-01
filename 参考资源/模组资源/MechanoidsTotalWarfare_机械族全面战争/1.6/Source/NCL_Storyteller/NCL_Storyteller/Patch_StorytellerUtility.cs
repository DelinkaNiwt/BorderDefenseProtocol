using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NCL_Storyteller;

[HarmonyPatch(typeof(StorytellerUtility))]
internal class Patch_StorytellerUtility
{
	[HarmonyPatch("DefaultThreatPointsNow")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		List<CodeInstruction> list = instructions.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].opcode == OpCodes.Ldc_R4 && list[i].OperandIs(0f) && list[i + 1].opcode == OpCodes.Stloc_3)
			{
				Log.Message("Find");
				list.RemoveAt(i);
				List<CodeInstruction> collection = new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldloc_1),
					new CodeInstruction(OpCodes.Call, NCL_StorytellerUtility.GetAdditionWealthCurveValue_Method)
				};
				list.InsertRange(i, collection);
			}
			if (list[i].opcode == OpCodes.Ldc_R4 && list[i].OperandIs(10000f))
			{
				list.Replace(list[i], new CodeInstruction(OpCodes.Call, NCL_StorytellerUtility.MaxDefaultThreatPointsNow_Method));
			}
		}
		return list;
	}
}
