using BDP.Content.FloatingFlight.Pathing;
using HarmonyLib;
using Unity.Collections;
using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Patches
{
    /// <summary>把原版终点矩形中的非法停留格排除出浮空路径。</summary>
    [HarmonyPatch(typeof(PathFinder), "MakeDestination")]
    internal static class Patch_PathFinder_MakeDestination_FloatingFlight
    {
        /// <summary>保留原版终点矩形，仅追加地面和屋顶终点过滤。</summary>
        private static void Postfix(
            LocalTargetInfo target,
            PathEndMode peMode,
            PathingContext context,
            ref CellRect rect,
            ref NativeList<int> excluded)
        {
            if (!FloatingFlightPathUtility.IsFloatingContext(context) || context.map == null)
            {
                return;
            }

            FloatingFlightPathKind kind = ResolveKind(context);
            CellIndices indices = context.map.cellIndices;
            foreach (IntVec3 cell in rect.Cells)
            {
                bool insideTouchTarget = (peMode == PathEndMode.Touch
                        || peMode == PathEndMode.ClosestTouch)
                    && target.HasThing
                    && target.Thing.OccupiedRect().Contains(cell);
                if (insideTouchTarget
                    || !cell.InBounds(context.map)
                    || !FloatingFlightPathUtility.IsLegalLandingCell(
                        context.map,
                        cell,
                        kind))
                {
                    int index = indices.CellToIndex(cell);
                    if (!Contains(excluded, index))
                    {
                        excluded.Add(index);
                    }
                }
            }
        }

        /// <summary>从正式 PathGrid Def 反推出当前浮空网格种类。</summary>
        private static FloatingFlightPathKind ResolveKind(PathingContext context)
        {
            return context.pathGrid.def == FloatingFlightDefOf.BDP_PathGrid_FloatingAlways
                ? FloatingFlightPathKind.Always
                : FloatingFlightPathKind.Hybrid;
        }

        /// <summary>避免重复写入原版 excluded 列表。</summary>
        private static bool Contains(NativeList<int> excluded, int value)
        {
            for (int index = 0; index < excluded.Length; index++)
            {
                if (excluded[index] == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
