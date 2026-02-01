using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace GD3
{
	[HarmonyPatch(typeof(Pawn_FlightTracker), "get_PositionOffsetFactor")]
	public static class FlyingRender_Patch
	{
		public static void Postfix(Pawn ___pawn, ref float __result)
		{
			IsFlyingUnit ext = ___pawn.def.GetModExtension<IsFlyingUnit>();
			if (ext != null)
			{
				__result *= ext.flyingHeight;
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_FlightTracker), "Notify_JobStarted")]
	public static class Notify_JobStarted_Patch
	{
		public static bool Prefix(Pawn_FlightTracker __instance, Pawn ___pawn, ref Job job)
		{
			if (___pawn.IsFlyingMech())
			{
				IsFlyingUnit ext = ___pawn.def.GetModExtension<IsFlyingUnit>();
				if (ext.flightDeterminedByCode)
                {
					return false;
                }
				if ((job.def.ifFlyingKeepFlying || GDUtility.shouldFlyJobDefs.Contains(job.def)) && __instance.Flying)
                {
					return false;
                }
				if (((___pawn.IsColonyMechPlayerControlled && ___pawn.Drafted) || (___pawn.IsColonyMechPlayerControlled && GDUtility.shouldFlyJobDefs.Contains(job.def))) && __instance.CanFlyNow && !__instance.Flying)
				{
					typeof(Pawn_FlightTracker).GetMethod("StartFlyingInternal", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, null);
					job.flying = true;
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(AttackTargetFinder), "FindBestReachableMeleeTarget")]
	public static class FindBestReachableMeleeTarget_Patch
	{
		public static void Prefix(ref Predicate<IAttackTarget> validator)
		{
			Predicate<IAttackTarget> old = validator;
			Predicate<IAttackTarget> another = delegate (IAttackTarget t)
			{
				if (t.Thing != null && t.Thing is Pawn p && p.IsFlyingMech() && p.Flying)
                {
					return false;
                }
				return old(t);
			};
			validator = another;
		}
	}

	[HarmonyPatch(typeof(Pawn), "ThreatDisabled")]
	public static class ThreatDisabled_Patch
	{
		public static void Postfix(Pawn __instance, IAttackTargetSearcher disabledFor, ref bool __result)
		{
			if (__instance.IsFlyingMech())
            {
				if (__instance is Annihilator annihilator && annihilator.Dying)
                {
					__result = true;
					return;
                }
				if (__instance.Flying && disabledFor?.CurrentEffectiveVerb != null && disabledFor.CurrentEffectiveVerb.IsMeleeAttack)
                {
					Pawn pawn = disabledFor as Pawn;
					if (pawn != null && pawn.Flying)
                    {
						return;
                    }
					__result = true;
                }
            }
		}
	}

	[HarmonyPatch(typeof(Pawn), "get_Drafted")]
	public static class FlyerDrafted_Patch
	{
		public static bool Prefix(Pawn __instance, ref bool __result)
		{
			if (__instance.IsFlyingMech() && !__instance.IsColonyMechPlayerControlled)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn), "TryGetAttackVerb")]
	public static class TryGetAttackVerb_Patch
	{
		public static bool Prefix(Pawn __instance, ref Verb __result)
		{
			if (__instance.IsFlyingMech() && __instance.kindDef == GDDefOf.Mech_Mosquito)
			{
				Ability ability = __instance.abilities.GetAbility(GDDefOf.MosquitoBombardment);
				if (ability != null && ability.CanCast && __instance.Flying)
				{
					__result = ability.verb;
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(JobGiver_AIFightEnemy), "MeleeAttackJob")]
	public static class ReplaceMeleeAttack_Patch
	{
		public static bool Prefix(Pawn pawn, Thing enemyTarget, ref Job __result)
		{
			if (pawn.IsFlyingMech() && pawn.Flying)
			{
				Job jobWait = JobMaker.MakeJob(JobDefOf.Wait_Combat, 20, checkOverrideOnExpiry: true);
				jobWait.overrideFacing = pawn.GetRot(enemyTarget.DrawPos);
				jobWait.forceMaintainFacing = true;
				__result = jobWait;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Projectile), "get_HitFlags")]
	public static class AntiAirProjectile_Patch
	{
		public static void Postfix(Projectile __instance, ref ProjectileHitFlags __result)
		{
			if (__instance.def.HasModExtension<IsAntiAirProj>() && __instance.def.projectile.flyOverhead)
			{
				__result = ProjectileHitFlags.IntendedTarget;
			}
		}
	}

	[HarmonyPatch(typeof(JobDriver_Wait), "<MakeNewToils>b__3_1")]
	public static class LogBlocker_FlyerWaitCombat_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			Log.Message("Glitterworld Destroyer: Trying Drafted...");
			var findInfo = AccessTools.Method(typeof(Pawn), "get_Drafted");
			var insertInfo = AccessTools.Method(typeof(LogBlocker_FlyerWaitCombat_Patch), "IfNotFlyer");
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
				list[target + 16].labels.Add(label);
				var codes = new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					CodeInstruction.LoadField(typeof(JobDriver), "pawn"),
					new CodeInstruction(OpCodes.Call, insertInfo),
					new CodeInstruction(OpCodes.Brfalse_S, label),
				};

				list.InsertRange(target + 2, codes);
			}
			return list;
		}

		public static bool IfNotFlyer(Pawn pawn)
		{
			if (pawn != null && pawn.IsFlyingMech())
			{
				return false;
			}
			return true;
		}
	}
}