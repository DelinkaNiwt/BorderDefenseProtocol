using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;

namespace GD3
{
	#region 机械湮黾
	[HarmonyPatch(typeof(PawnRenderer), "DrawShadowInternal")]
	public static class Annihilator_ShadowRender_Patch
	{
		public static bool Prefix(Pawn ___pawn)
		{
			if (!ModsConfig.OdysseyActive)
            {
				return true;
            }
			if (___pawn.def == GDDefOf.Mech_Annihilator)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn_FlightTracker), "CheckFlyAnimation")]
	public static class Annihilator_AnimationChecker_Patch
	{
		public static bool Prefix(Pawn ___pawn)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return true;
			}
			if (___pawn.def == GDDefOf.Mech_Annihilator)
			{
				return false;
			}
			return true;
		}
	}


	[HarmonyPatch(typeof(Pawn_FlightTracker), "ForceLand")]
	public static class Annihilator_ForceLand_Patch
	{
		public static void Postfix(Pawn ___pawn, ref int ___lerpTick)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return;
			}
			if (___pawn.def == GDDefOf.Mech_Annihilator)
			{
				if (___lerpTick == 25)
                {
					___lerpTick = 0;
                }
			}
		}
	}

	[HarmonyPatch(typeof(PawnUtility), "ShouldCollideWithPawns")]
	public static class Annihilator_ShouldCollideWithPawns_Patch
	{
		public static bool Prefix(Pawn p, ref bool __result)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return true;
			}
			if (p == null || p.DeadOrDowned)
			{
				return true;
			}
			if (p.def == GDDefOf.Mech_Annihilator && p.Flying)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn_MeleeVerbs), "TryGetMeleeVerb")]
	public static class Annihilator_TryGetMeleeVerb_Patch
	{
		public static bool Prefix(Pawn_MeleeVerbs __instance, ref Verb __result)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return true;
			}
			if (__instance.Pawn?.def == GDDefOf.Mech_Annihilator)
			{
				__result = null;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Building_MechCharger), "get_WireMaterial")]
	public static class Annihilator_EaseInOutQuart_Patch
	{
		public static bool Prefix(Building_MechCharger __instance, ref Material __result)
		{
			if (__instance.def.defName == "CentralRecharger")
			{
				__result = Building_CentralCharger.WireMaterial;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ThingListGroupHelper), "Includes")]
	public static class Annihilator_ThingListGroupHelper_Patch
	{
		public static bool Prefix(ThingRequestGroup group, ThingDef def, ref bool __result)
		{
			if (group == ThingRequestGroup.MechCharger && def.defName == "CentralRecharger")
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(MechanitorUtility), "IngredientsFromDisassembly")]
	public static class Annihilator_Ingredients_Patch
	{
		public static bool Prefix(ThingDef mech, ref List<ThingDefCountClass> __result)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return true;
			}
			if (mech == GDDefOf.Mech_Annihilator)
			{
				List<ThingDefCountClass> tmpIngredients = new List<ThingDefCountClass>();
				ThingDef allDef = GDDefOf.PlayerBuilding_Annihilator;
				for (int i = 0; i < allDef.costList.Count; i++)
				{
					ThingDef thingDef = allDef.costList[i].thingDef;
					int count = Mathf.Max(1, Mathf.RoundToInt(allDef.costList[i].count * 0.4f));
					tmpIngredients.Add(new ThingDefCountClass(thingDef, count));
				}
				__result = tmpIngredients;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(JobDriver_DisassembleMech))]
	public static class Annihilator_DisassembleMech_Patch
	{
		[HarmonyTargetMethod]
		public static MethodInfo TargetMethod()
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(JobDriver_DisassembleMech), "<MakeNewToils>b__5_1");
			return methodInfo;
		}
		[HarmonyPrefix]
		public static bool Prefix(JobDriver_DisassembleMech __instance)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return true;
			}
			Pawn Mech = typeof(JobDriver_DisassembleMech).GetMethod("get_Mech", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, null) as Pawn;
			if (Mech?.def == GDDefOf.Mech_Annihilator)
			{
				foreach (ThingDefCountClass item in MechanitorUtility.IngredientsFromDisassembly(Mech.def))
				{
					Thing thing = ThingMaker.MakeThing(item.thingDef);
					thing.stackCount = item.count;
					GenPlace.TryPlaceThing(thing, Mech.Position, Mech.Map, ThingPlaceMode.Near);
				}
				Mech.forceNoDeathNotification = true;
				Mech.Kill(null, null);
				Mech.forceNoDeathNotification = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(GravshipPlacementUtility))]
	public static class Annihilator_GravshipPlacement_Patch
	{
		[HarmonyTargetMethod]
		public static MethodInfo TargetMethod()
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(GravshipPlacementUtility), "ClearThingsAt");
			return methodInfo;
		}
		[HarmonyPrefix]
		public static bool Prefix(IntVec3 cell, Map map)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return true;
			}
			List<Thing> list = map.thingGrid.ThingsListAt(cell);
			if (list.Any(t => t.def.defName == "GD_AnnihilatorTerminal"))
            {
				return false;
            }
			return true;
		}
	}

	[HarmonyPatch(typeof(PawnCapacityUtility), "CalculateCapacityLevel")]
	public static class PawnCapacityUtility_Patch
	{
		public static void Postfix(ref float __result, HediffSet diffSet, PawnCapacityDef capacity)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return;
			}
			if (diffSet.pawn.def == GDDefOf.Mech_Annihilator)
			{
				if ((capacity == PawnCapacityDefOf.Manipulation || capacity == PawnCapacityDefOf.Moving) && __result < 0.4f)
				{
					__result = 0.4f;
				}
			}
		}
	}

	[HarmonyPatch(typeof(CompOrbitalScanner), "get_ScannerQuests")]
	public static class Annihilator_Quest_Patch
	{
		public static void Postfix(CompOrbitalScanner __instance, ref List<QuestScriptDef> __result)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return;
			}
			Slate slate = new Slate();
			slate.Set("points", 1000f);
			slate.Set("discoveryMethod", "QuestDiscoveredFromOrbitalScanner".Translate());

			List<QuestScriptDef> list = __result;

			if (!list.Any(d => d.defName == "GD_Quest_StarSlayer"))
            {
				list.Add(DefDatabase<QuestScriptDef>.GetNamed("GD_Quest_StarSlayer"));
            }
			list.RemoveAll(q => q.defName == "GD_Quest_StarSlayer" && !q.CanRun(slate, __instance.parent?.Map as IIncidentTarget ?? Find.World));

			Log.Message(list.ToStringSafeEnumerable());
			__result = list;
		}
	}

	[HarmonyPatch(typeof(Pawn_PathFollower), "PatherTick")]
	public static class Annihilator_CanMove_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message("Glitterworld Destroyer: Trying...");
			var findInfo = AccessTools.Field(typeof(Pawn_PathFollower), "debugDisabled");
			var insertInfo = AccessTools.Method(typeof(Annihilator_CanMove_Patch), "NotAnnihilatorOrNotFlying");
			var list = instructions.ToList();
			int target = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].LoadsField(findInfo))
				{
					target = i;
					break;
				}
			}

			if (target != -1)
			{
				var label = generator.DefineLabel();
				list[target - 1].labels.Add(label);
				var codes = new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					CodeInstruction.LoadField(typeof(Pawn_PathFollower), "pawn"),
					new CodeInstruction(OpCodes.Call, insertInfo),
					new CodeInstruction(OpCodes.Brfalse_S, label),
				};

				list.InsertRange(target - 6, codes);
			}
			return list;
		}

		public static bool NotAnnihilatorOrNotFlying(Pawn pawn)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return true;
			}
			if (pawn.def == GDDefOf.Mech_Annihilator)
			{
				return !pawn.Flying;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(RoofCollapserImmediate), "DropRoofInCellPhaseOne")]
	public static class Annihilator_RoofCollapse_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message("Glitterworld Destroyer: Trying...");
			var findInfo = AccessTools.Method(typeof(GenSpawn), "Spawn", new Type[] { typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(WipeMode) });
			var insertInfo = AccessTools.Method(typeof(Annihilator_RoofCollapse_Patch), "NotAnnihilator");
			var list = instructions.ToList();
			int target = -1;
			int subTarget = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Calls(findInfo))
				{
					target = i - 7;
					subTarget = i;
					break;
				}
			}

			if (target != -1)
			{
				var label = generator.DefineLabel();
				list[subTarget + 9].labels.Add(label);
				var codes = new List<CodeInstruction>
				{
					CodeInstruction.LoadArgument(0),
					CodeInstruction.LoadArgument(1),
					new CodeInstruction(OpCodes.Call, insertInfo),
					new CodeInstruction(OpCodes.Brfalse_S, label),
				};

				list.InsertRange(subTarget - 5, codes);
			}
			return list;
		}

		public static bool NotAnnihilator(IntVec3 cell, Map map)
		{
			if (!ModsConfig.OdysseyActive)
			{
				return true;
			}
			List<Thing> list = map.thingGrid.ThingsListAt(cell);
			if (!list.NullOrEmpty() && list.Any(t => t.def == GDDefOf.Mech_Annihilator))
			{
				return false;
			}
			return true;
		}
	}
	#endregion
}
