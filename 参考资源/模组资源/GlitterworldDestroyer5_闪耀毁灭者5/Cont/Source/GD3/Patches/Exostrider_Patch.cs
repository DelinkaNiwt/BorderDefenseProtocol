using System;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System.Reflection;

namespace GD3
{
	[HarmonyPatch(typeof(TurretTop), "TurretTopTick")]
	public static class Exostrider_Patch
	{
		public static bool Prefix(TurretTop __instance, Building_Turret ___parentTurret)
		{
			if (___parentTurret != null && ___parentTurret.def.defName == "GD_Exostrider")
            {
                LocalTargetInfo currentTarget = ___parentTurret.CurrentTarget;
                if (currentTarget.IsValid)
                {
                    __instance.CurRotation = (currentTarget.Cell.ToVector3Shifted() - ___parentTurret.DrawPos).AngleFlat();
                }
				return false;
            }
			return true;
		}
	}

	[HarmonyPatch(typeof(Skyfaller), "DrawAt")]
	public static class WarningDraw_Patch
	{
		public static void Postfix(Skyfaller __instance)
		{
			if (__instance.def == GDDefOf.ExostriderShell_Down)
            {
				CompDrawWarning comp = __instance.TryGetComp<CompDrawWarning>();
				comp.PostDraw();
            }
		}
	}
}