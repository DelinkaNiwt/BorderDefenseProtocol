using BDP.Content.FloatingFlight.Pathing;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Patches
{
    /// <summary>露天浮空穿越门时跳过原版开门、等待和破门代价。</summary>
    [HarmonyPatch(typeof(PathUtility), "GetDoorCost")]
    internal static class Patch_PathUtility_GetDoorCost_FloatingFlight
    {
        /// <summary>只把露天实际飞行路径上的门视为零代价通行。</summary>
        private static bool Prefix(
            Building_Door door,
            TraverseParms traverseParms,
            Pawn pawn,
            PathFinderCostTuning? tuningParams,
            ref ushort __result)
        {
            if (door == null
                || pawn == null
                || pawn.Map == null
                || !FloatingFlightUtility.IsActuallyFlying(pawn)
                || !door.Position.InBounds(pawn.Map)
                || door.Position.Roofed(pawn.Map))
            {
                return true;
            }

            __result = 0;
            return false;
        }
    }
}
