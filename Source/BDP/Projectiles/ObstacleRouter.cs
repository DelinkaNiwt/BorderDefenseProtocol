using System.Collections.Generic;
using BDP.Projectiles.Modules;
using UnityEngine;
using Verse;

namespace BDP.Projectiles
{
    /// <summary>
    /// 障碍物自动绕行路由结果。
    /// LeftAnchors/RightAnchors 为 null 表示该侧不可绕行。
    /// </summary>
    public struct ObstacleRouteResult
    {
        public List<IntVec3> LeftAnchors;
        public List<IntVec3> RightAnchors;
        public bool IsValid => (LeftAnchors != null && LeftAnchors.Count > 0)
                            || (RightAnchors != null && RightAnchors.Count > 0);
    }

    /// <summary>
    /// 障碍物自动绕行路由——静态工具类。
    /// 检测射手→目标射线上的障碍物，BFS扩展连通区域，
    /// 提取外侧轮廓，叉积分左右两侧，每侧选最多5个关键锚点。
    /// 齐射时交替分配左/右路径实现子弹自动分叉绕行。
    /// </summary>
    public static class ObstacleRouter
    {
        /// <summary>BFS扩展障碍物连通区域的格子上限。</summary>
        private const int MAX_OBSTACLE_CELLS = 200;

        // 8方向偏移（含对角线）
        private static readonly IntVec3[] Neighbors8 = new IntVec3[]
        {
            new IntVec3(1, 0, 0), new IntVec3(-1, 0, 0),
            new IntVec3(0, 0, 1), new IntVec3(0, 0, -1),
            new IntVec3(1, 0, 1), new IntVec3(1, 0, -1),
            new IntVec3(-1, 0, 1), new IntVec3(-1, 0, -1)
        };

        /// <summary>轮廓点：记录格子坐标、沿轴投影距离、离轴距离。</summary>
        private struct ContourPoint
        {
            public IntVec3 Cell;
            public float Projection;
            public float LateralDist;
        }

        // ══════════════════════════════════════════
        //  主入口
        // ══════════════════════════════════════════

        /// <summary>
        /// 计算绕行路由。返回null表示无需/无法绕行。
        /// </summary>
        public static ObstacleRouteResult? ComputeRoute(
            IntVec3 shooterPos, IntVec3 targetPos, Map map,
            int anchorsPerWall = 5)
        {
            // Step1: 沿射线找第一个阻挡格
            IntVec3? blockCell = FindFirstBlockingCell(shooterPos, targetPos, map);
            if (!blockCell.HasValue) return null;

            // Step2: BFS扩展障碍物连通区域
            HashSet<IntVec3> obstacleCells = BfsExpandObstacle(blockCell.Value, map);

            // Step3: 提取轮廓
            List<IntVec3> contour = ExtractContour(obstacleCells, map);
            if (contour.Count == 0) return null;

            // Step4: 叉积分左右两侧
            float axisX = targetPos.x - shooterPos.x;
            float axisZ = targetPos.z - shooterPos.z;
            float axisLen = Mathf.Sqrt(axisX * axisX + axisZ * axisZ);
            if (axisLen < 1f) return null;

            float normX = axisX / axisLen;
            float normZ = axisZ / axisLen;

            var leftPoints = new List<ContourPoint>();
            var rightPoints = new List<ContourPoint>();

            for (int i = 0; i < contour.Count; i++)
            {
                float dx = contour[i].x - shooterPos.x;
                float dz = contour[i].z - shooterPos.z;
                float proj = dx * normX + dz * normZ;
                // 排除射手/目标背后的点
                if (proj < 1f || proj > axisLen - 1f) continue;

                float cross = normX * dz - normZ * dx;
                float lateralDist = Mathf.Abs(cross);
                var cp = new ContourPoint
                {
                    Cell = contour[i],
                    Projection = proj,
                    LateralDist = lateralDist
                };

                if (cross > 0f) leftPoints.Add(cp);
                else if (cross < 0f) rightPoints.Add(cp);
            }

            // Step5: 每侧分段选锚点（贪心取LateralDist最大点）
            List<IntVec3> leftAnchors = SelectAnchors(leftPoints, anchorsPerWall);
            List<IntVec3> rightAnchors = SelectAnchors(rightPoints, anchorsPerWall);

            if ((leftAnchors == null || leftAnchors.Count == 0)
                && (rightAnchors == null || rightAnchors.Count == 0))
                return null;

            var result = new ObstacleRouteResult
            {
                LeftAnchors = leftAnchors,
                RightAnchors = rightAnchors
            };

            // 诊断：输出锚点坐标 + 相邻锚点间LOS检查（已移至VerbFlightState统一输出）
            // if (TrackingDiag.Enabled)
            //     LogRouteResult(result, shooterPos, targetPos, map);

            return result;
        }

        // ══════════════════════════════════════════
        //  Step1: 射线检测
        // ══════════════════════════════════════════

        /// <summary>沿Bresenham射线逐格检查，找第一个不可视穿的格子。</summary>
        private static IntVec3? FindFirstBlockingCell(
            IntVec3 from, IntVec3 to, Map map)
        {
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(from, to))
            {
                if (cell == from) continue;
                if (cell == to) break;
                if (!cell.InBounds(map)) break;
                if (!cell.CanBeSeenOverFast(map))
                    return cell;
            }
            return null;
        }

        // ══════════════════════════════════════════
        //  Step2: BFS扩展
        // ══════════════════════════════════════════

        /// <summary>
        /// 从种子格BFS扩展（8方向），收集相连的不可视穿格子。
        /// 上限MAX_OBSTACLE_CELLS格。自写BFS避免嵌套冲突。
        /// </summary>
        private static HashSet<IntVec3> BfsExpandObstacle(IntVec3 seed, Map map)
        {
            var visited = new HashSet<IntVec3>();
            var queue = new Queue<IntVec3>();
            visited.Add(seed);
            queue.Enqueue(seed);

            while (queue.Count > 0 && visited.Count < MAX_OBSTACLE_CELLS)
            {
                IntVec3 current = queue.Dequeue();
                for (int i = 0; i < Neighbors8.Length; i++)
                {
                    IntVec3 neighbor = current + Neighbors8[i];
                    if (!neighbor.InBounds(map)) continue;
                    if (visited.Contains(neighbor)) continue;
                    if (!neighbor.CanBeSeenOverFast(map))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return visited;
        }

        // ══════════════════════════════════════════
        //  Step3: 提取轮廓
        // ══════════════════════════════════════════

        /// <summary>
        /// 障碍格的8邻居中可通行的格子即为轮廓。
        /// </summary>
        private static List<IntVec3> ExtractContour(
            HashSet<IntVec3> obstacleCells, Map map)
        {
            var contourSet = new HashSet<IntVec3>();
            foreach (IntVec3 obs in obstacleCells)
            {
                for (int i = 0; i < Neighbors8.Length; i++)
                {
                    IntVec3 neighbor = obs + Neighbors8[i];
                    if (!neighbor.InBounds(map)) continue;
                    if (!obstacleCells.Contains(neighbor)
                        && neighbor.CanBeSeenOverFast(map))
                        contourSet.Add(neighbor);
                }
            }
            return new List<IntVec3>(contourSet);
        }

        // ══════════════════════════════════════════
        //  迭代绕行 & 路径LOS校验（供VerbFlightState/AnchorTargetingHelper共用）
        // ══════════════════════════════════════════

        /// <summary>
        /// 迭代分段绕行路径构建。
        /// 从 from 出发，对"当前末点→to"段反复调用 ComputeRoute，
        /// 最多 maxDepth 次，直到全段通路或深度耗尽。
        /// preferLeft=true 时优先选左侧锚点，失败降级右侧；反之亦然。
        /// 返回完整锚点列表（不含端点），无法绕行时返回 null。
        /// </summary>
        public static List<IntVec3> ComputeIterativeRoute(
            IntVec3 from, IntVec3 to, Map map,
            int maxDepth, int anchorsPerWall, bool preferLeft)
        {
            var allAnchors = new List<IntVec3>();
            IntVec3 segFrom = from;

            for (int depth = 0; depth < maxDepth; depth++)
            {
                // 当前段已通，不再迭代
                if (GenSight.LineOfSight(segFrom, to, map))
                    break;

                var seg = ComputeRoute(segFrom, to, map, anchorsPerWall);
                if (seg == null || !seg.Value.IsValid) return null;

                // 优先选一侧，不可用时降级到另一侧
                List<IntVec3> side = preferLeft
                    ? (seg.Value.LeftAnchors  ?? seg.Value.RightAnchors)
                    : (seg.Value.RightAnchors ?? seg.Value.LeftAnchors);
                if (side == null || side.Count == 0) return null;

                allAnchors.AddRange(side);
                segFrom = side[side.Count - 1];  // 下次从末锚点继续
            }

            return allAnchors.Count > 0 ? allAnchors : null;
        }

        /// <summary>
        /// 逐段LOS检查：shooterPos→锚点1→…→锚点N→targetPos，任一段不通即返回false。
        /// anchors为null或空时返回false。
        /// </summary>
        public static bool IsPathClear(
            IntVec3 shooterPos, List<IntVec3> anchors, IntVec3 targetPos, Map map)
        {
            if (anchors == null || anchors.Count == 0) return false;
            // 射手→首锚点
            if (!GenSight.LineOfSight(shooterPos, anchors[0], map)) return false;
            // 锚点间逐段
            for (int i = 0; i < anchors.Count - 1; i++)
            {
                if (!GenSight.LineOfSight(anchors[i], anchors[i + 1], map)) return false;
            }
            // 末锚点→目标
            if (!GenSight.LineOfSight(anchors[anchors.Count - 1], targetPos, map)) return false;
            return true;
        }

        // ══════════════════════════════════════════
        //  Step5: 分段选锚点
        // ══════════════════════════════════════════

        /// <summary>
        /// 按投影距离排序后分5段（每段20%），
        /// 每段取LateralDist最大的单点（原始贪心），
        /// 按投影顺序组装。返回1~5个IntVec3，或null。
        /// </summary>
        private static List<IntVec3> SelectAnchors(List<ContourPoint> points, int segmentCount)
        {
            if (points == null || points.Count == 0) return null;

            points.Sort((a, b) => a.Projection.CompareTo(b.Projection));

            float minProj = points[0].Projection;
            float maxProj = points[points.Count - 1].Projection;
            float range = maxProj - minProj;

            // 障碍物太窄，只取离轴最远的一个点
            if (range < 0.5f)
            {
                points.Sort((a, b) => b.LateralDist.CompareTo(a.LateralDist));
                return new List<IntVec3> { points[0].Cell };
            }

            // 按segmentCount等分，每段取LateralDist最大点
            float segSize = range / segmentCount;

            // 每段取LateralDist最大的点
            var bestPerSeg = new ContourPoint?[segmentCount];
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                int seg = Mathf.Clamp(
                    Mathf.FloorToInt((p.Projection - minProj) / segSize),
                    0, segmentCount - 1);

                if (!bestPerSeg[seg].HasValue
                    || p.LateralDist > bestPerSeg[seg].Value.LateralDist)
                {
                    bestPerSeg[seg] = p;
                }
            }

            // 按段序组装，去重
            var anchors = new List<IntVec3>();
            for (int s = 0; s < segmentCount; s++)
            {
                if (!bestPerSeg[s].HasValue) continue;
                IntVec3 cell = bestPerSeg[s].Value.Cell;
                if (!anchors.Contains(cell))
                    anchors.Add(cell);
            }

            return anchors.Count > 0 ? anchors : null;
        }

        // ══════════════════════════════════════════
        //  诊断日志
        // ══════════════════════════════════════════

        /// <summary>输出路由结果：锚点坐标 + 相邻段LOS检查。</summary>
        private static void LogRouteResult(
            ObstacleRouteResult result, IntVec3 shooter, IntVec3 target, Map map)
        {
            LogSideAnchors("L", result.LeftAnchors, shooter, target, map);
            LogSideAnchors("R", result.RightAnchors, shooter, target, map);
        }

        private static void LogSideAnchors(
            string side, List<IntVec3> anchors, IntVec3 shooter, IntVec3 target, Map map)
        {
            if (anchors == null || anchors.Count == 0) return;

            // 构建完整路径：射手 → 锚点1 → ... → 锚点N → 目标
            var path = new List<IntVec3> { shooter };
            path.AddRange(anchors);
            path.Add(target);

            var sb = new System.Text.StringBuilder();
            sb.Append($"[BDP-Route] {side}侧 ");
            bool hasBlind = false;
            for (int i = 0; i < path.Count - 1; i++)
            {
                bool los = GenSight.LineOfSight(path[i], path[i + 1], map);
                if (!los) hasBlind = true;
                string tag = los ? "✓" : "✗";
                sb.Append(i == 0 ? $"S" : $"A{i}");
                sb.Append($"-{tag}-");
            }
            sb.Append("T");
            if (hasBlind)
                Log.Warning(sb.ToString());
            else
                Log.Message(sb.ToString());
        }
    }
}
