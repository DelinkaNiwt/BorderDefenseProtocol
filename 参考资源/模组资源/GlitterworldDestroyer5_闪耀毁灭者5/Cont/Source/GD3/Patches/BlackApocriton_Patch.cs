using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
	[HarmonyPatch(typeof(Pawn), "Kill")]
	public static class BlackApocriton_Kill_Patch
	{
		public static void Postfix(Pawn __instance)
		{
			bool flag = __instance.Dead;
			if (flag)
			{
				CompBlackApocriton comp = __instance.GetComp<CompBlackApocriton>();
				bool flag2 = comp != null;
				if (flag2)
				{
					Find.World.GetComponent<MissionComponent>().apocritonDead = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
	public static class BlackApocriton_SBDowned_Patch
	{
		public static bool Prefix(Pawn ___pawn, ref bool __result)
		{
			if (___pawn.def == GDDefOf.Mech_BlackApocriton || ___pawn.def == GDDefOf.Mech_Annihilator)
            {
				__result = false;
				return false;
            }
			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDead")]
	public static class BlackApocriton_SBDead_Patch
	{
		public static bool Prefix(Pawn ___pawn, ref bool __result)
		{
			if (___pawn.def == GDDefOf.Mech_BlackApocriton || ___pawn.def == GDDefOf.Mech_Annihilator)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Verb))]
	public static class BlackApocriton_SwapVictim_Patch
	{
		[HarmonyTargetMethod]
		public static MethodInfo TargetMethod()
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(Verb), "TryStartCastOn", new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) });
			return methodInfo;
		}
		[HarmonyPostfix]
		public static void Postfix(Verb __instance, LocalTargetInfo castTarg)
		{
			if (!(__instance is Verb_LaunchProjectile))
            {
				return;
            }
			Pawn caster = __instance.CasterPawn;
			if (caster != null && castTarg.Pawn != null && castTarg.Pawn is BlackApocriton apo)
			{
				apo.TrySwapVictim(caster, __instance);
			}
		}
	}
}
