using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Edited_BM_WeaponSummon;

[HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
public static class JobGiver_AIFightEnemy_TryGiveJob_Patch
{
	public static void Postfix(ref Job __result, Pawn pawn)
	{
		if (__result != null)
		{
			TrySwapToWeaponSummon(ref __result, pawn);
		}
	}

	private static void TrySwapToWeaponSummon(ref Job __result, Pawn pawn)
	{
		if (pawn?.equipment?.Primary?.GetComp<CompSummonedWeapon>() != null)
		{
			return;
		}
		foreach (AbilityDef item in DefDatabase<AbilityDef>.AllDefs.InRandomOrder())
		{
			List<AbilityCompProperties> comps = item.comps;
			if (comps == null || !comps.OfType<CompProperties_SummonWeapon>().Any())
			{
				continue;
			}
			try
			{
				JobGiver_AICastSummonWeapon obj = new JobGiver_AICastSummonWeapon();
				typeof(JobGiver_AICastAbility).GetField("ability", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(obj, item);
				Job job = (Job)(typeof(JobGiver_AICastAbility).GetMethod("TryGiveJob", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(obj, new object[1] { pawn }));
				if (job != null)
				{
					__result = job;
					break;
				}
			}
			catch (Exception arg)
			{
				Log.Error($"[武器召唤] 创建任务失败: {arg}");
			}
		}
	}
}
