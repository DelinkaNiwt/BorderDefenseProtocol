using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.RoutePath
{
    /// <summary>
    /// 路线自动绕障结果。
    /// </summary>
    public sealed class RouteAutoResult
    {
        /// <summary>自动路径是否成功。</summary>
        public bool Succeeded { get; private set; }

        /// <summary>默认中间锚点列表。</summary>
        public List<IntVec3> Anchors { get; private set; } = new List<IntVec3>();

        /// <summary>左侧候选锚点。</summary>
        public List<IntVec3> LeftAnchors { get; private set; } = new List<IntVec3>();

        /// <summary>右侧候选锚点。</summary>
        public List<IntVec3> RightAnchors { get; private set; } = new List<IntVec3>();

        /// <summary>失败原因。</summary>
        public string RejectReason { get; private set; }

        public static RouteAutoResult Success(List<IntVec3> anchors)
        {
            return new RouteAutoResult
            {
                Succeeded = true,
                Anchors = CloneAnchors(anchors),
                LeftAnchors = new List<IntVec3>(),
                RightAnchors = new List<IntVec3>(),
                RejectReason = null
            };
        }

        public static RouteAutoResult Success(List<IntVec3> leftAnchors, List<IntVec3> rightAnchors)
        {
            List<IntVec3> left = CloneAnchors(leftAnchors);
            List<IntVec3> right = CloneAnchors(rightAnchors);
            return new RouteAutoResult
            {
                Succeeded = true,
                Anchors = CloneAnchors(SelectDefaultAnchors(left, right)),
                LeftAnchors = left,
                RightAnchors = right,
                RejectReason = null
            };
        }

        public static RouteAutoResult Failure(string rejectReason)
        {
            return new RouteAutoResult
            {
                Succeeded = false,
                Anchors = new List<IntVec3>(),
                LeftAnchors = new List<IntVec3>(),
                RightAnchors = new List<IntVec3>(),
                RejectReason = string.IsNullOrWhiteSpace(rejectReason)
                    ? "BDP_RoutePath_AutoRouteFailed".Translate().ToString()
                    : rejectReason
            };
        }

        private static List<IntVec3> CloneAnchors(IReadOnlyList<IntVec3> anchors)
        {
            List<IntVec3> clone = new List<IntVec3>();
            if (anchors == null) return clone;
            for (int i = 0; i < anchors.Count; i++) clone.Add(anchors[i]);
            return clone;
        }

        private static IReadOnlyList<IntVec3> SelectDefaultAnchors(
            IReadOnlyList<IntVec3> leftAnchors,
            IReadOnlyList<IntVec3> rightAnchors)
        {
            if (leftAnchors != null && leftAnchors.Count > 0) return leftAnchors;
            return rightAnchors ?? new List<IntVec3>();
        }
    }

    /// <summary>
    /// 路线自动绕障解析器。
    /// 把阻挡在直射线上的障碍团转成普通路径锚点。
    /// </summary>
    public static class RouteAutoResolver
    {
        private static readonly IntVec3[] Neighbors8 =
        {
            new IntVec3(1, 0, 0), new IntVec3(-1, 0, 0),
            new IntVec3(0, 0, 1), new IntVec3(0, 0, -1),
            new IntVec3(1, 0, 1), new IntVec3(1, 0, -1),
            new IntVec3(-1, 0, 1), new IntVec3(-1, 0, -1)
        };

        private struct ContourPoint
        {
            public IntVec3 Cell;
            public float Projection;
            public float LateralDistance;
        }

        private struct RouteOptions
        {
            public List<IntVec3> LeftAnchors;
            public List<IntVec3> RightAnchors;
            public bool IsValid
            {
                get
                {
                    return (LeftAnchors != null && LeftAnchors.Count > 0)
                        || (RightAnchors != null && RightAnchors.Count > 0);
                }
            }
        }

        public static RouteAutoResult TryResolve(
            Map map, IntVec3 originCell, IntVec3 targetCell, RoutePathConfig config)
        {
            if (map == null || !originCell.IsValid || !targetCell.IsValid
                || !originCell.InBounds(map) || !targetCell.InBounds(map))
                return RouteAutoResult.Failure("BDP_RoutePath_AutoRouteOriginTargetInvalid".Translate());

            if (config != null && !config.EnableAutoRoute)
                return RouteAutoResult.Failure("BDP_RoutePath_AutoRouteDisabled".Translate());

            if (GenSight.LineOfSight(originCell, targetCell, map))
                return RouteAutoResult.Success(new List<IntVec3>());

            int maxDepth = ResolveMaxDepth(config);
            int anchorsPerWall = ResolveAnchorsPerWall(config);
            int maxObstacleCells = ResolveMaxObstacleCells(config);

            string leftRejectReason;
            List<IntVec3> leftRoute = ComputeIterativeRoute(
                originCell, targetCell, map, maxDepth, anchorsPerWall, maxObstacleCells, true, out leftRejectReason);

            string rightRejectReason;
            List<IntVec3> rightRoute = ComputeIterativeRoute(
                originCell, targetCell, map, maxDepth, anchorsPerWall, maxObstacleCells, false, out rightRejectReason);

            if (leftRoute != null && !ValidateRoute(originCell, leftRoute, targetCell, map))
            { leftRoute = null; leftRejectReason = "BDP_RoutePath_AutoRouteFailed".Translate(); }

            if (rightRoute != null && !ValidateRoute(originCell, rightRoute, targetCell, map))
            { rightRoute = null; rightRejectReason = "BDP_RoutePath_AutoRouteFailed".Translate(); }

            bool hasLeft = leftRoute != null && leftRoute.Count > 0;
            bool hasRight = rightRoute != null && rightRoute.Count > 0;
            if (hasLeft || hasRight)
                return RouteAutoResult.Success(leftRoute, rightRoute);

            string rejectReason = !string.IsNullOrWhiteSpace(leftRejectReason) ? leftRejectReason : rightRejectReason;
            return RouteAutoResult.Failure(rejectReason);
        }

        private static List<IntVec3> ComputeIterativeRoute(
            IntVec3 originCell, IntVec3 targetCell, Map map,
            int maxDepth, int anchorsPerWall, int maxObstacleCells,
            bool preferLeft, out string rejectReason)
        {
            rejectReason = null;
            List<IntVec3> anchors = new List<IntVec3>();
            IntVec3 currentOrigin = originCell;

            for (int depth = 0; depth < maxDepth; depth++)
            {
                if (GenSight.LineOfSight(currentOrigin, targetCell, map))
                    return anchors.Count > 0 ? anchors : null;

                RouteOptions routeOptions = ComputeSingleObstacleRoute(
                    currentOrigin, targetCell, map, anchorsPerWall, maxObstacleCells, out rejectReason);
                if (!routeOptions.IsValid) return null;

                List<IntVec3> selectedSide = SelectPreferredSide(routeOptions, preferLeft);
                if (selectedSide == null || selectedSide.Count <= 0)
                { rejectReason = "BDP_RoutePath_NoDetourPath".Translate(); return null; }

                AppendDistinctAnchors(anchors, selectedSide);
                currentOrigin = anchors[anchors.Count - 1];
            }

            if (!GenSight.LineOfSight(currentOrigin, targetCell, map))
            { rejectReason = "BDP_RoutePath_AutoRouteFailed".Translate(); return null; }

            return anchors.Count > 0 ? anchors : null;
        }

        private static RouteOptions ComputeSingleObstacleRoute(
            IntVec3 originCell, IntVec3 targetCell, Map map,
            int anchorsPerWall, int maxObstacleCells, out string rejectReason)
        {
            rejectReason = null;
            IntVec3 blockingCell;
            if (!FindFirstBlockingCell(originCell, targetCell, map, out blockingCell))
            { rejectReason = "BDP_RoutePath_AutoRouteFailed".Translate(); return new RouteOptions(); }

            HashSet<IntVec3> obstacleCluster = CollectObstacleCluster(blockingCell, map, maxObstacleCells);
            List<IntVec3> contourCandidates = CollectContourCandidates(obstacleCluster, map);
            if (contourCandidates.Count <= 0)
            { rejectReason = "BDP_RoutePath_NoDetourPath".Translate(); return new RouteOptions(); }

            return BuildRouteOptions(originCell, targetCell, contourCandidates, anchorsPerWall);
        }

        private static bool FindFirstBlockingCell(
            IntVec3 originCell, IntVec3 targetCell, Map map, out IntVec3 blockingCell)
        {
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(originCell, targetCell))
            {
                if (cell == originCell) continue;
                if (cell == targetCell) break;
                if (!cell.InBounds(map)) break;

                if (IsBlockingCell(cell, map)) { blockingCell = cell; return true; }
            }
            blockingCell = IntVec3.Invalid;
            return false;
        }

        private static HashSet<IntVec3> CollectObstacleCluster(IntVec3 seed, Map map, int maxObstacleCells)
        {
            HashSet<IntVec3> visited = new HashSet<IntVec3>();
            Queue<IntVec3> pending = new Queue<IntVec3>();
            if (!seed.InBounds(map) || !IsBlockingCell(seed, map)) return visited;

            visited.Add(seed);
            pending.Enqueue(seed);
            while (pending.Count > 0 && visited.Count < maxObstacleCells)
            {
                IntVec3 current = pending.Dequeue();
                for (int i = 0; i < Neighbors8.Length; i++)
                {
                    IntVec3 neighbor = current + Neighbors8[i];
                    if (!neighbor.InBounds(map) || visited.Contains(neighbor) || !IsBlockingCell(neighbor, map))
                        continue;
                    visited.Add(neighbor);
                    pending.Enqueue(neighbor);
                }
            }
            return visited;
        }

        private static List<IntVec3> CollectContourCandidates(HashSet<IntVec3> obstacleCluster, Map map)
        {
            HashSet<IntVec3> contourSet = new HashSet<IntVec3>();
            if (obstacleCluster == null || obstacleCluster.Count <= 0) return new List<IntVec3>();

            foreach (IntVec3 obstacleCell in obstacleCluster)
            {
                for (int i = 0; i < Neighbors8.Length; i++)
                {
                    IntVec3 neighbor = obstacleCell + Neighbors8[i];
                    if (neighbor.InBounds(map) && !obstacleCluster.Contains(neighbor) && IsUsableAnchorCell(neighbor, map))
                        contourSet.Add(neighbor);
                }
            }
            return new List<IntVec3>(contourSet);
        }

        private static RouteOptions BuildRouteOptions(
            IntVec3 originCell, IntVec3 targetCell,
            List<IntVec3> contourCandidates, int anchorsPerWall)
        {
            float axisX = targetCell.x - originCell.x;
            float axisZ = targetCell.z - originCell.z;
            float axisLength = Mathf.Sqrt(axisX * axisX + axisZ * axisZ);
            if (axisLength < 1f) return new RouteOptions();

            float normalX = axisX / axisLength;
            float normalZ = axisZ / axisLength;
            List<ContourPoint> leftPoints = new List<ContourPoint>();
            List<ContourPoint> rightPoints = new List<ContourPoint>();

            for (int i = 0; i < contourCandidates.Count; i++)
            {
                IntVec3 candidate = contourCandidates[i];
                float dx = candidate.x - originCell.x;
                float dz = candidate.z - originCell.z;
                float projection = dx * normalX + dz * normalZ;
                if (projection < 1f || projection > axisLength - 1f) continue;

                float cross = normalX * dz - normalZ * dx;
                if (Mathf.Abs(cross) < 0.01f) continue;

                ContourPoint point = new ContourPoint
                { Cell = candidate, Projection = projection, LateralDistance = Mathf.Abs(cross) };
                if (cross > 0f) leftPoints.Add(point); else rightPoints.Add(point);
            }

            return new RouteOptions
            {
                LeftAnchors = SelectAnchors(leftPoints, anchorsPerWall),
                RightAnchors = SelectAnchors(rightPoints, anchorsPerWall)
            };
        }

        private static List<IntVec3> SelectAnchors(List<ContourPoint> points, int segmentCount)
        {
            if (points == null || points.Count <= 0 || segmentCount <= 0) return null;

            points.Sort(CompareContourPointByProjection);
            float minProjection = points[0].Projection;
            float maxProjection = points[points.Count - 1].Projection;
            float projectionRange = maxProjection - minProjection;
            if (projectionRange < 0.5f)
            { points.Sort(CompareContourPointByLateralDistance); return new List<IntVec3> { points[0].Cell }; }

            float segmentSize = projectionRange / segmentCount;
            ContourPoint?[] bestPerSegment = new ContourPoint?[segmentCount];
            for (int i = 0; i < points.Count; i++)
            {
                ContourPoint point = points[i];
                int segmentIndex = Mathf.Clamp(
                    Mathf.FloorToInt((point.Projection - minProjection) / segmentSize), 0, segmentCount - 1);
                if (!bestPerSegment[segmentIndex].HasValue
                    || CompareContourPointByLateralDistance(point, bestPerSegment[segmentIndex].Value) < 0)
                    bestPerSegment[segmentIndex] = point;
            }

            List<IntVec3> anchors = new List<IntVec3>();
            for (int i = 0; i < bestPerSegment.Length; i++)
            {
                if (!bestPerSegment[i].HasValue) continue;
                IntVec3 cell = bestPerSegment[i].Value.Cell;
                if (!anchors.Contains(cell)) anchors.Add(cell);
            }
            return anchors.Count > 0 ? anchors : null;
        }

        private static List<IntVec3> SelectPreferredSide(RouteOptions routeOptions, bool preferLeft)
        {
            if (preferLeft)
                return routeOptions.LeftAnchors != null && routeOptions.LeftAnchors.Count > 0
                    ? routeOptions.LeftAnchors : routeOptions.RightAnchors;
            return routeOptions.RightAnchors != null && routeOptions.RightAnchors.Count > 0
                ? routeOptions.RightAnchors : routeOptions.LeftAnchors;
        }

        private static bool ValidateRoute(IntVec3 originCell, IReadOnlyList<IntVec3> anchors,
            IntVec3 targetCell, Map map)
        {
            if (anchors == null || anchors.Count <= 0) return false;
            IntVec3 currentOrigin = originCell;
            for (int i = 0; i < anchors.Count; i++)
            {
                IntVec3 anchor = anchors[i];
                if (!IsUsableAnchorCell(anchor, map) || !GenSight.LineOfSight(currentOrigin, anchor, map))
                    return false;
                currentOrigin = anchor;
            }
            return GenSight.LineOfSight(currentOrigin, targetCell, map);
        }

        private static void AppendDistinctAnchors(List<IntVec3> target, List<IntVec3> source)
        {
            if (target == null || source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                IntVec3 anchor = source[i];
                if (target.Count > 0 && target[target.Count - 1] == anchor) continue;
                target.Add(anchor);
            }
        }

        private static bool IsBlockingCell(IntVec3 cell, Map map)
            => cell.InBounds(map) && !cell.CanBeSeenOverFast(map);

        private static bool IsUsableAnchorCell(IntVec3 cell, Map map)
            => cell.IsValid && cell.InBounds(map) && cell.Walkable(map) && cell.CanBeSeenOverFast(map);

        private static int CompareContourPointByProjection(ContourPoint left, ContourPoint right)
        {
            int cmp = left.Projection.CompareTo(right.Projection);
            if (cmp != 0) return cmp;
            int xc = left.Cell.x.CompareTo(right.Cell.x);
            return xc != 0 ? xc : left.Cell.z.CompareTo(right.Cell.z);
        }

        private static int CompareContourPointByLateralDistance(ContourPoint left, ContourPoint right)
        {
            int cmp = right.LateralDistance.CompareTo(left.LateralDistance);
            if (cmp != 0) return cmp;
            return CompareContourPointByProjection(left, right);
        }

        private static int ResolveMaxDepth(RoutePathConfig config)
            => config != null && config.AutoRouteMaxDepth > 0 ? config.AutoRouteMaxDepth : 3;

        private static int ResolveAnchorsPerWall(RoutePathConfig config)
            => config != null && config.AutoRouteAnchorsPerWall > 0 ? config.AutoRouteAnchorsPerWall : 3;

        private static int ResolveMaxObstacleCells(RoutePathConfig config)
            => config != null && config.AutoRouteMaxObstacleCells > 0 ? config.AutoRouteMaxObstacleCells : 200;
    }
}
