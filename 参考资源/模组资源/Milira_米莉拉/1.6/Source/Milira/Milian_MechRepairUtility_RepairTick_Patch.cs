using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(MechRepairUtility))]
[HarmonyPatch("RepairTick")]
public static class Milian_MechRepairUtility_RepairTick_Patch
{
	[HarmonyPostfix]
	public static bool Prefix(Pawn mech, int delta)
	{
		if (MilianUtility.IsMilian(mech))
		{
			Hediff hediffToHeal = Milian_MechRepairUtility_CanRepair_Patch.GetHediffToHeal(mech);
			if (hediffToHeal != null)
			{
				if (hediffToHeal is Hediff_MissingPart hediff)
				{
					mech.health.RemoveHediff(hediff);
				}
				else
				{
					hediffToHeal.Heal(delta);
				}
			}
			List<Apparel> wornApparel = mech.apparel.WornApparel;
			foreach (Apparel item in wornApparel)
			{
				if (item.HitPoints < item.MaxHitPoints)
				{
					item.HitPoints += delta;
					if (item.HitPoints > item.MaxHitPoints)
					{
						item.HitPoints = item.MaxHitPoints;
					}
				}
				else
				{
					item.HitPoints = item.MaxHitPoints;
				}
			}
			return false;
		}
		return true;
	}
}
