using BDP.Content.FloatingFlight.Pathing;
using HarmonyLib;
using Unity.Collections;
using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Patches
{
    /// <summary>
    /// 把 BDP 浮空网格接入 RimWorld 1.6 实际的 PathGridJob。
    /// 仅替换当前请求的直接代价数组，不改写原版全局 PathGrid。
    /// </summary>
    [HarmonyPatch(typeof(PathFinderMapData), nameof(PathFinderMapData.ParameterizeGridJob))]
    internal static class Patch_PathFinderMapData_ParameterizeGridJob_FloatingFlight
    {
        /// <summary>
        /// 原版参数化完成后，把当前浮空 PathGrid 的缓存数组交给真实网格作业。
        /// </summary>
        private static void Postfix(
            PathRequest request,
            ref PathGridJob job)
        {
            if (request == null
                || !FloatingFlightPathGridCustomizer.TryGetKind(
                    request.customizer,
                    out FloatingFlightPathKind kind))
            {
                return;
            }

            PathingContext context = FloatingFlightPathUtility.GetContext(
                request.map,
                kind);
            NativeArray<int> grid = context?.pathGrid?.Grid_Unsafe
                ?? default(NativeArray<int>);
            if (!grid.IsCreated)
            {
                return;
            }

            job.pathGridDirect = grid.AsReadOnly();
        }
    }

    /// <summary>给浮空 PathRequest 添加区分路径网格的缓存身份。</summary>
    [HarmonyPatch]
    internal static class Patch_PathFinder_CreateRequest_FloatingFlight
    {
        /// <summary>定位带 TraverseParms 的 PathFinder.CreateRequest 重载。</summary>
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(PathFinder),
                nameof(PathFinder.CreateRequest),
                new[]
                {
                    typeof(IntVec3),
                    typeof(LocalTargetInfo),
                    typeof(IntVec3?),
                    typeof(TraverseParms),
                    typeof(PathFinderCostTuning?),
                    typeof(PathEndMode),
                    typeof(Pawn),
                    typeof(PathRequest.IPathGridCustomizer)
                });
        }

        /// <summary>保留已有定制器并固定本次请求的浮空路径种类。</summary>
        private static void Postfix(PathRequest __result)
        {
            if (__result == null || __result.pawn == null)
            {
                return;
            }

            FloatingFlightPathKind kind = FloatingFlightPathUtility.ResolvePathKind(__result.pawn);
            if (kind == FloatingFlightPathKind.Vanilla
                || FloatingFlightPathGridCustomizer.TryGetKind(
                    __result.customizer,
                    out FloatingFlightPathKind _))
            {
                return;
            }

            __result.customizer = FloatingFlightPathGridCustomizer.Wrap(
                kind,
                __result.customizer);
        }
    }
}
