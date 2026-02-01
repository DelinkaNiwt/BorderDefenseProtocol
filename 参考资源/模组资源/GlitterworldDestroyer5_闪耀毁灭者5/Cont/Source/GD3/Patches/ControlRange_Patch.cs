using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace GD3
{
    /*[HarmonyPatch(
    typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.CanCommandTo), new Type[]{typeof(LocalTargetInfo)})]
    static class CanCommandToPatch
    {
        static bool Prefix(Pawn_MechanitorTracker __instance, LocalTargetInfo target, ref bool __result)
        {
            __result = true;
            return false;
        }
    }*/

    [HarmonyPatch(
    typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.DrawCommandRadius), new Type[]{ })]
    static class DrawRadiusPatch
    {
        static bool Prefix(Pawn_MechanitorTracker __instance)
        {
            Pawn pawn = __instance.Pawn;
            if (pawn.Spawned)
            {
                Hediff RangeLink = pawn.health?.hediffSet?.GetFirstHediffOfDef(GDDefOf.GD_ControlRangelinkImplant);
                if (RangeLink != null)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MechanitorUtility),"InMechanitorCommandRange")]
    static class InMechanitorCommandRangePatch
    {
        public static bool Prefix(Pawn mech, LocalTargetInfo target, ref bool __result)
        {
            if (GDUtility.MissionComponent.IsSavingMech(mech))
            {
                __result = true;
                return false;
            }
            Pawn overseer = mech.GetOverseer();
            if (overseer != null)
            {
                Hediff RangeLink = overseer.health?.hediffSet?.GetFirstHediffOfDef(GDDefOf.GD_ControlRangelinkImplant);
                if (RangeLink != null)
                {
                    int level = RangeLink.CurStageIndex;
                    if (level == 0)
                    {
                        if (mech.MapHeld == overseer.MapHeld)
                        {
                            __result = true;
                            return false;
                        }
                        if (mech.MapHeld != overseer.MapHeld)
                        {
                            __result = false;
                            return false;
                        }
                    }
                    else if (level == 1)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            return true;
        }
    }

}