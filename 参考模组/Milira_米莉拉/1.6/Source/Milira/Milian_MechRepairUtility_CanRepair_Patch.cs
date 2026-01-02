using System.Collections.Generic;
using AncotLibrary;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(MechRepairUtility))]
[HarmonyPatch("CanRepair")]
public static class Milian_MechRepairUtility_CanRepair_Patch
{
	[HarmonyPostfix]
	public static void Postfix(ref bool __result, Pawn mech)
	{
		if (MilianUtility.IsMilian(mech) && GetHediffToHeal(mech) == null)
		{
			__result = AnyApparelDamaged(mech);
		}
		if (((Thing)mech).TryGetComp<CompDrone>() != null)
		{
			__result = GetHediffToHeal(mech) != null;
		}
	}

	public static bool AnyApparelDamaged(Pawn mech)
	{
		List<Apparel> wornApparel = mech.apparel.WornApparel;
		foreach (Apparel item in wornApparel)
		{
			if (item.HitPoints < item.MaxHitPoints)
			{
				return true;
			}
		}
		return false;
	}

	public static Hediff GetHediffToHeal(Pawn mech)
	{
		Hediff hediff = null;
		float num = float.PositiveInfinity;
		foreach (Hediff hediff2 in mech.health.hediffSet.hediffs)
		{
			if (hediff2 is Hediff_Injury && hediff2.Severity < num)
			{
				num = hediff2.Severity;
				hediff = hediff2;
			}
		}
		if (hediff != null)
		{
			return hediff;
		}
		foreach (Hediff hediff3 in mech.health.hediffSet.hediffs)
		{
			if (hediff3 is Hediff_MissingPart)
			{
				return hediff3;
			}
		}
		return null;
	}
}
