using BDP.Content.FloatingFlight;
using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Pathing
{
    /// <summary>
    /// 浮空路径的格子可达性实现。
    /// 这里只负责“能否连通”和终点合法性，实际路径代价与占位冲突仍交给原版 PathFinder。
    /// </summary>
    public static class FloatingFlightReachability
    {
        /// <summary>检查指定 Pawn 是否能以当前浮空网格到达目标。</summary>
        public static bool CanReach(
            Pawn pawn,
            IntVec3 start,
            LocalTargetInfo target,
            PathEndMode peMode,
            TraverseParms traverseParams)
        {
            if (pawn == null || pawn.Map == null || pawn.Map != target.Thing?.MapHeld && target.HasThing)
            {
                return false;
            }

            Map map = pawn.Map;
            if (!start.InBounds(map) || !target.IsValid || !target.Cell.InBounds(map))
            {
                return false;
            }

            FloatingFlightPathKind kind = FloatingFlightPathUtility.ResolvePathKind(pawn);
            if (kind == FloatingFlightPathKind.Vanilla)
            {
                return false;
            }

            target = (LocalTargetInfo)GenPath.ResolvePathMode(
                pawn,
                target.ToTargetInfo(map),
                ref peMode);

            PathingContext context = FloatingFlightPathUtility.GetContext(map, kind);
            if (context == null || context.pathGrid == null)
            {
                return false;
            }

            Area allowedArea = PathUtility.GetAllowedArea(pawn);
            bool reached = false;
            System.Predicate<IntVec3> passCheck = cell =>
                cell == start || IsTraversableCell(cell, pawn, context, allowedArea, traverseParams);
            System.Func<IntVec3, bool> processor = cell =>
            {
                if (!CanEndAt(pawn, target, peMode, cell, kind))
                {
                    return false;
                }

                reached = true;
                return true;
            };

            map.floodFiller.FloodFill(start, passCheck, processor);
            return reached;
        }

        /// <summary>检查格子是否满足浮空路径的终点条件。</summary>
        public static bool CanEndAt(
            Pawn pawn,
            LocalTargetInfo target,
            PathEndMode peMode,
            IntVec3 cell,
            FloatingFlightPathKind kind)
        {
            Map map = pawn.Map;
            if (map == null || !cell.InBounds(map))
            {
                return false;
            }

            if (!FloatingFlightPathUtility.IsLegalLandingCell(map, cell, kind))
            {
                return false;
            }

            switch (peMode)
            {
                case PathEndMode.OnCell:
                    return cell == target.Cell;
                case PathEndMode.Touch:
                case PathEndMode.ClosestTouch:
                    if (!target.HasThing)
                    {
                        return cell == target.Cell;
                    }

                    CellRect occupied = target.Thing.OccupiedRect();
                    return occupied.ExpandedBy(1).Contains(cell) && !occupied.Contains(cell);
                default:
                    return cell == target.Cell;
            }
        }

        /// <summary>保留原版允许区域、危险等级和当前浮空网格的通行约束。</summary>
        private static bool IsTraversableCell(
            IntVec3 cell,
            Pawn pawn,
            PathingContext context,
            Area allowedArea,
            TraverseParms traverseParams)
        {
            Map map = pawn.Map;
            if (!cell.InBounds(map) || !context.pathGrid.WalkableFast(cell))
            {
                return false;
            }

            if (allowedArea != null && !allowedArea[cell])
            {
                return false;
            }

            return (int)cell.GetDangerFor(pawn, map) <= (int)traverseParams.maxDanger;
        }
    }
}
