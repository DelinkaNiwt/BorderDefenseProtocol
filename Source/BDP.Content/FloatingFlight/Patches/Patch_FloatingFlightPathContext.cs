using BDP.Content.FloatingFlight.Pathing;
using HarmonyLib;
using Verse;
using Verse.AI;
using VersePathing = Verse.AI.Pathing;

namespace BDP.Content.FloatingFlight.Patches
{
    /// <summary>
    /// 仅为 BDP 浮空 Pawn 替换原版寻路上下文。
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_FloatingFlightPathContext
    {
        /// <summary>替换 Pawn.GetPathContext 的结果。</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetPathContext))]
        private static void PawnGetPathContextPostfix(
            Pawn __instance,
            VersePathing pathing,
            ref PathingContext __result)
        {
            FloatingFlightPathKind kind = FloatingFlightPathUtility.ResolvePathKind(__instance);
            if (kind != FloatingFlightPathKind.Vanilla)
            {
                __result = FloatingFlightPathUtility.GetContext(__instance.Map, kind);
            }
        }

        /// <summary>替换带 Pawn 的 TraverseParms（遍历参数）上下文。</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VersePathing), nameof(VersePathing.For), new[] { typeof(TraverseParms) })]
        private static void PathingForTraverseParmsPostfix(
            TraverseParms parms,
            ref PathingContext __result)
        {
            Pawn pawn = parms.pawn;
            FloatingFlightPathKind kind = FloatingFlightPathUtility.ResolvePathKind(pawn);
            if (kind != FloatingFlightPathKind.Vanilla)
            {
                __result = FloatingFlightPathUtility.GetContext(pawn.Map, kind);
            }
        }
    }
}
