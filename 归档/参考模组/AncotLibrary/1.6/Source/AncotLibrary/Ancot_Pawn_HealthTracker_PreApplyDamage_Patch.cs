using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HarmonyLib;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(Pawn_HealthTracker))]
[HarmonyPatch("PreApplyDamage")]
public static class Ancot_Pawn_HealthTracker_PreApplyDamage_Patch
{
	private static readonly Func<Pawn_HealthTracker, Pawn> GetPawn = CreatePawnGetter();

	private static Func<Pawn_HealthTracker, Pawn> CreatePawnGetter()
	{
		ParameterExpression parameterExpression = Expression.Parameter(typeof(Pawn_HealthTracker), "instance");
		MemberExpression body = Expression.Field(parameterExpression, "pawn");
		Expression<Func<Pawn_HealthTracker, Pawn>> expression = Expression.Lambda<Func<Pawn_HealthTracker, Pawn>>(body, new ParameterExpression[1] { parameterExpression });
		return expression.Compile();
	}

	[HarmonyPrefix]
	public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, ref bool absorbed)
	{
		Pawn pawn = GetPawn(__instance);
		ThingWithComps thingWithComps = pawn.equipment?.Primary;
		if (thingWithComps != null)
		{
			List<ThingComp> allComps = thingWithComps.AllComps;
			for (int i = 0; i < allComps.Count; i++)
			{
				allComps[i].PostPreApplyDamage(ref dinfo, out absorbed);
				if (absorbed)
				{
					return false;
				}
			}
		}
		return true;
	}
}
