using BDP.Content.FloatingFlight.Pathing;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Patches
{
    /// <summary>只拦截携带 Pawn 的浮空 Reachability.CanReach 请求。</summary>
    [HarmonyPatch(
        typeof(Reachability),
        nameof(Reachability.CanReach),
        new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) })]
    internal static class Patch_Reachability_CanReach_FloatingFlight
    {
        /// <summary>让浮空 Pawn 使用格子 FloodFill，其他请求继续走原版。</summary>
        private static bool Prefix(
            IntVec3 start,
            LocalTargetInfo dest,
            PathEndMode peMode,
            TraverseParms traverseParams,
            ref bool __result)
        {
            Pawn pawn = traverseParams.pawn;
            if (pawn == null || FloatingFlightPathUtility.ResolvePathKind(pawn) == FloatingFlightPathKind.Vanilla)
            {
                return true;
            }

            __result = FloatingFlightReachability.CanReach(
                pawn,
                start,
                dest,
                peMode,
                traverseParams);
            return false;
        }
    }
}
