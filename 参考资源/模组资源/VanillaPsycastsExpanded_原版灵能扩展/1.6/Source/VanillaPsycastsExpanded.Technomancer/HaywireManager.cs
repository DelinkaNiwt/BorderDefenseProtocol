using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded.Technomancer;

[HarmonyPatch]
[StaticConstructorOnStartup]
public class HaywireManager
{
	[HarmonyPatch]
	public static class OverrideBestAttackTargetValidator
	{
		[HarmonyTargetMethod]
		public static MethodInfo TargetMethod()
		{
			return AccessTools.Method(AccessTools.Inner(typeof(AttackTargetFinder), "<>c__DisplayClass5_0"), "<BestAttackTarget>b__1");
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			List<CodeInstruction> list = instructions.ToList();
			MethodInfo info = AccessTools.Method(typeof(GenHostility), "HostileTo", new Type[2]
			{
				typeof(Thing),
				typeof(Thing)
			});
			int startIndex = list.FindIndex((CodeInstruction ins) => ins.Calls(info));
			int num = list.FindLastIndex(startIndex, (CodeInstruction ins) => ins.opcode == OpCodes.Ldarg_0);
			FieldInfo operand = (FieldInfo)list[num + 1].operand;
			int index = list.FindIndex(startIndex, (CodeInstruction ins) => ins.opcode == OpCodes.Ldc_I4_0);
			list.RemoveAt(index);
			list.InsertRange(index, new CodeInstruction[3]
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, operand),
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HaywireManager), "ShouldTargetAllies"))
			});
			return list;
		}
	}

	public static readonly HashSet<Thing> HaywireThings;

	static HaywireManager()
	{
		HaywireThings = new HashSet<Thing>();
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (typeof(Building_Turret).IsAssignableFrom(allDef.thingClass))
			{
				allDef.comps.Add(new CompProperties(typeof(CompHaywire)));
			}
		}
	}

	public static bool ShouldTargetAllies(Thing t)
	{
		return HaywireThings.Contains(t);
	}

	[HarmonyPatch(typeof(AttackTargetsCache), "GetPotentialTargetsFor")]
	[HarmonyPostfix]
	public static void ChangeTargets(IAttackTargetSearcher th, ref List<IAttackTarget> __result, AttackTargetsCache __instance)
	{
		if (th is Thing item && HaywireThings.Contains(item))
		{
			__result.Clear();
			__result.AddRange(__instance.TargetsHostileToColony);
		}
	}

	[HarmonyPatch(typeof(Building_TurretGun), "IsValidTarget")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		List<CodeInstruction> list = instructions.ToList();
		FieldInfo info = AccessTools.Field(typeof(Building_TurretGun), "mannableComp");
		int num = list.FindIndex((CodeInstruction ins) => ins.LoadsField(info));
		Label label = (Label)list[num + 1].operand;
		int num2 = list.FindLastIndex(num, (CodeInstruction ins) => ins.opcode == OpCodes.Ldarg_0);
		list.InsertRange(num2 + 1, new CodeInstruction[3]
		{
			new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HaywireManager), "ShouldTargetAllies")),
			new CodeInstruction(OpCodes.Brtrue, label),
			new CodeInstruction(OpCodes.Ldarg_0)
		});
		return list;
	}
}
