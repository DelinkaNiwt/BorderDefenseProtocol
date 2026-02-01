using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace GD3
{
	[HarmonyPatch(typeof(MechRepairUtility), "RepairTick")]
	public static class MechRepair_Patch
	{
		public static void Postfix(Pawn mech)
		{
			CompHitArmor comp = mech.TryGetComp<CompHitArmor>();
			if (comp != null)
			{
				comp.Notify_RepairMech();
			}
		}
	}

	[HarmonyPatch(typeof(MechRepairUtility), "CanRepair")]
	public static class MechCanRepair_Patch
	{
		public static bool Prefix(Pawn mech, ref bool __result)
		{
			CompHitArmor comp = mech.TryGetComp<CompHitArmor>();
			if (comp != null && comp.CanRepair)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}
}