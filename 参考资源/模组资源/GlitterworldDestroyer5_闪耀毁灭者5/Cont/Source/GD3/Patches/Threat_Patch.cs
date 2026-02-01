using System;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Cecil.Cil;
using Verse.AI.Group;

namespace GD3
{
	[HarmonyPatch(typeof(IncidentWorker_Raid), "AdjustedRaidPoints")]
	public static class Threat_Patch
	{
		public static void Postfix(Faction faction, ref float __result)
		{
			if (faction == Faction.OfMechanoids)
            {
				__result *= GDSettings.threatRate * 0.01f * GDSettings.threatFactorPostfix;
			}
			if (GDSettings.DeveloperMode)
            {
				Log.Warning("points:" + __result + " | " + "faction:" + faction.Name);
            }
		}
	}

	[HarmonyPatch(typeof(MechClusterGenerator), "GenerateClusterSketch")]
	public static class ThreatCluster_Patch
	{
		public static void Prefix(ref float points)
		{
			points *= GDSettings.threatRate * 0.01f;
			if (GDSettings.DeveloperMode)
			{
				Log.Warning("cluster points:" + points);
			}
		}
	}

	[HarmonyPatch(typeof(IncidentWorker_Raid), "PostProcessSpawnedPawns")]
	public static class AddReinforce_Patch
	{
		public static void Postfix(IncidentParms parms, List<Pawn> pawns)
		{
			if (parms.faction == Faction.OfMechanoids && parms.points > 2000 && parms.target is Map map && map.IsPlayerHome && GDUtility.CanReinforce(map))
			{
				if (!pawns.NullOrEmpty() && pawns.TryRandomElement(p => p.equipment?.PrimaryEq?.PrimaryVerb != null && !p.equipment.PrimaryEq.PrimaryVerb.IsMeleeAttack, out Pawn result))
                {
					result.health.AddHediff(GDDefOf.GD_CallReinforcement);
                }
			}
		}
	}

	[HarmonyPatch(typeof(Building), "ClaimableBy")]
	public static class ClaimableBy_Patch
	{
		public static bool Prefix(Building __instance, ref AcceptanceReport __result)
		{
			if ((__instance.Faction == Faction.OfMechanoids || __instance.Faction == GDUtility.BlackMechanoid) && __instance.Map != null && !__instance.Map.IsPlayerHome)
            {
				__result = false;
				return false;
            }
			return true;
		}
	}

	#region 黑衣毒蜂可以看到隐身单位
	[HarmonyPatch(typeof(Pawn), "ThreatDisabled")]
	public static class ThreatDisabled_ForBlackApocriton_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message("Glitterworld Destroyer: Trying...");
			var findInfo = AccessTools.Method(typeof(InvisibilityUtility), "IsPsychologicallyInvisible");
			var insertInfo = AccessTools.Method(typeof(ThreatDisabled_ForBlackApocriton_Patch), "IfNotBlackApocriton");
			var list = instructions.ToList();
			int target = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Calls(findInfo))
				{
					target = i;
					break;
				}
			}

			if (target != -1)
			{
				var label = generator.DefineLabel();
				list[target + 4].labels.Add(label);
				var codes = new List<CodeInstruction>
				{
					CodeInstruction.LoadArgument(1),
					new CodeInstruction(OpCodes.Call, insertInfo),
					new CodeInstruction(OpCodes.Brfalse_S, label),
				};

				list.InsertRange(target + 2, codes);
			}
			return list;
		}

		public static bool IfNotBlackApocriton(IAttackTargetSearcher disabledFor)
		{
			Thing thing = disabledFor?.Thing;
			if (thing != null && thing.def == GDDefOf.Mech_BlackApocriton)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
	public static class AIFightEnemy_ForBlackApocriton_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message("Glitterworld Destroyer: Trying...");
			var findInfo = AccessTools.Method(typeof(InvisibilityUtility), "IsPsychologicallyInvisible");
			var insertInfo = AccessTools.Method(typeof(AIFightEnemy_ForBlackApocriton_Patch), "IfNotBlackApocriton");
			var list = instructions.ToList();
			int target = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Calls(findInfo))
				{
					target = i;
					break;
				}
			}

			if (target != -1)
			{
				var label = generator.DefineLabel();
				list[target + 4].labels.Add(label);
				var codes = new List<CodeInstruction>
				{
					CodeInstruction.LoadArgument(1),
					new CodeInstruction(OpCodes.Call, insertInfo),
					new CodeInstruction(OpCodes.Brfalse_S, label),
				};

				list.InsertRange(target + 2, codes);
			}
			return list;
		}

		public static bool IfNotBlackApocriton(Pawn pawn)
		{
			BlackApocriton p = pawn as BlackApocriton;
			if (p != null)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Verb), "CanHitTargetFrom")]
	public static class Verb_ForBlackApocriton_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message("Glitterworld Destroyer: Trying...");
			var findInfo = AccessTools.Method(typeof(InvisibilityUtility), "IsPsychologicallyInvisible");
			var insertInfo = AccessTools.Method(typeof(Verb_ForBlackApocriton_Patch), "IfNotBlackApocriton");
			var list = instructions.ToList();
			int target = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Calls(findInfo))
				{
					target = i;
					break;
				}
			}

			if (target != -1)
			{
				var label = generator.DefineLabel();
				list[target + 10].labels.Add(label);
				var codes = new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					CodeInstruction.LoadField(typeof(Verb), "caster"),
					new CodeInstruction(OpCodes.Call, insertInfo),
					new CodeInstruction(OpCodes.Brfalse_S, label),
				};

				list.InsertRange(target + 2, codes);
			}
			return list;
		}

		public static bool IfNotBlackApocriton(Thing thing)
		{
			BlackApocriton p = thing as BlackApocriton;
			if (p != null)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Toils_Combat))]
	public static class ToilsCombat_ForBlackApocriton_Patch
	{
		[HarmonyTargetMethod]
		public static MethodInfo TargetMethod()
		{
			MethodInfo methodInfo = AccessTools.Method(AccessTools.Inner(typeof(Toils_Combat), "<>c__DisplayClass6_0"), "<FollowAndMeleeAttack>b__0");
			return methodInfo;
		}
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message("Glitterworld Destroyer: Trying inner method...");
			var findInfo = AccessTools.Method(typeof(InvisibilityUtility), "IsPsychologicallyInvisible");
			var insertInfo = AccessTools.Method(typeof(ToilsCombat_ForBlackApocriton_Patch), "IfNotBlackApocriton");
			var list = instructions.ToList();
			int target = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Calls(findInfo))
				{
					target = i;
					break;
				}
			}

			if (target != -1)
			{
				var label = generator.DefineLabel();
				list[target + 5].labels.Add(label);
				var codes = new List<CodeInstruction>
				{
					CodeInstruction.LoadLocal(0),
					new CodeInstruction(OpCodes.Call, insertInfo),
					new CodeInstruction(OpCodes.Brfalse_S, label),
				};

				list.InsertRange(target + 2, codes);
			}
			return list;
		}

		public static bool IfNotBlackApocriton(Pawn pawn)
		{
			BlackApocriton p = pawn as BlackApocriton;
			if (p != null)
			{
				return false;
			}
			return true;
		}
	}
	#endregion

	#region 对话系统设定为拒绝交互时禁用大部分战斗指令
	[HarmonyPatch(typeof(Pawn), "ThreatDisabled")]
	public static class ThreatDisabled_Util_Patch
	{
		public static bool Prefix(ref bool __result)
		{
			if (GDUtility.ExtraDrawer.preventInteraction)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(FloatMenuOptionProvider_DraftedAttack), "CanTarget")]
	public static class ThreatDisabled_FloatMenu_Patch
	{
		public static bool Prefix(ref bool __result)
		{
			if (GDUtility.ExtraDrawer.preventInteraction)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public static class ThreatDisabled_VerbsCommands_Patch
	{
		public static bool Prefix(ref IEnumerable<Gizmo> __result)
		{
			if (GDUtility.ExtraDrawer.preventInteraction)
			{
				__result = new List<Gizmo>();
				return false;
			}
			return true;
		}
	}
	#endregion
}