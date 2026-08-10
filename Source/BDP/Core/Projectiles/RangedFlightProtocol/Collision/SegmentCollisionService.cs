using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Projection;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Collision
{
    /// <summary>
    /// 统一扫描单个飞行段穿过的格子，并识别首个客观阻挡体。
    /// 它只回答“这段真实穿过了哪些格、遇到了什么客观阻挡”，不读取业务模块私有状态。
    /// </summary>
    internal static class SegmentCollisionService
    {
        /// <summary>
        /// 曲线路径客观阻挡扫描时允许的最大折线步长。
        /// 步长越小，越不容易漏掉“曲线外鼓”碰到的墙格。
        /// </summary>
        private const float MaxCurveSampleStep = 0.25f;

        /// <summary>
        /// 首次进入阻挡格的二分逼近迭代次数。
        /// 这里只求稳定收敛到足够近的位置，不追求数学上的极限精度。
        /// </summary>
        private const int EntrySearchIterations = 12;

        /// <summary>
        /// 扫描当前飞行段，并返回穿格与客观阻挡事实。
        /// </summary>
        /// <param name="projectile">当前投射物宿主。</param>
        /// <param name="segmentStart">当前段起点。</param>
        /// <param name="segmentEnd">当前段终点。</param>
        /// <returns>当前段的客观碰撞扫描结果。</returns>
        public static SegmentCollisionRecord ScanSegment(Projectile projectile, Vector3 segmentStart, Vector3 segmentEnd)
        {
            ProjectileFlightPathSnapshot linearSnapshot = ProjectileFlightPathUtility.CreateLinear(segmentStart, segmentEnd);
            return ScanSegment(projectile, linearSnapshot);
        }

        /// <summary>
        /// 基于完整 flight path snapshot 扫描当前飞行段。
        /// 宿主层只提供中立几何快照，扫描服务负责按真实路径采样并收集穿格事实。
        /// </summary>
        /// <param name="projectile">当前投射物宿主。</param>
        /// <param name="flightPathSnapshot">当前段 flight path 几何快照。</param>
        /// <returns>当前段的客观碰撞扫描结果。</returns>
        public static SegmentCollisionRecord ScanSegment(Projectile projectile, ProjectileFlightPathSnapshot flightPathSnapshot)
        {
            return ScanSegment(projectile, flightPathSnapshot, 0f, 1f);
        }

        /// <summary>
        /// 基于 flight path snapshot 的局部进度区间扫描当前飞行段。
        /// 该重载主要服务于“真实命中前已经实际走过了哪一段曲线”的诊断口径。
        /// </summary>
        /// <param name="projectile">当前投射物宿主。</param>
        /// <param name="flightPathSnapshot">当前段 flight path 几何快照。</param>
        /// <param name="startProgress">扫描起始进度，范围 [0,1]。</param>
        /// <param name="endProgress">扫描终止进度，范围 [0,1]。</param>
        /// <returns>当前进度区间的客观碰撞扫描结果。</returns>
        public static SegmentCollisionRecord ScanSegment(
            Projectile projectile,
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float startProgress,
            float endProgress)
        {
            float clampedStartProgress = Mathf.Clamp01(startProgress);
            float clampedEndProgress = Mathf.Clamp01(endProgress);
            if (clampedEndProgress < clampedStartProgress)
            {
                float swap = clampedStartProgress;
                clampedStartProgress = clampedEndProgress;
                clampedEndProgress = swap;
            }

            SegmentCollisionRecord record = new SegmentCollisionRecord
            {
                PathKind = flightPathSnapshot != null
                    ? flightPathSnapshot.Kind
                    : ProjectileFlightPathKind.Linear
            };

            if (flightPathSnapshot == null)
            {
                return record;
            }

            List<Vector3> sampledPathPoints = SamplePathPoints(flightPathSnapshot, clampedStartProgress, clampedEndProgress);
            record.SamplePointCount = sampledPathPoints.Count;
            if (sampledPathPoints.Count == 0)
            {
                return record;
            }

            record.SegmentStart = sampledPathPoints[0];
            record.SegmentEnd = sampledPathPoints[sampledPathPoints.Count - 1];

            List<IntVec3> traversedCells = EnumerateTraversedCells(sampledPathPoints);
            record.TraversedCells.AddRange(traversedCells);

            Map map = projectile?.Map;
            if (map == null)
            {
                return record;
            }

            TryPopulateFirstObjectiveBlockerFacts(
                projectile,
                flightPathSnapshot,
                clampedStartProgress,
                clampedEndProgress,
                traversedCells,
                record);

            return record;
        }

        /// <summary>
        /// 按真实路径采样 flight path snapshot，并返回按飞行顺序排列的采样点。
        /// 线性路径只保留首尾两点，曲线路径则按固定最大步长转成折线。
        /// </summary>
        /// <param name="flightPathSnapshot">当前段几何快照。</param>
        /// <param name="startProgress">采样起始进度，范围 [0,1]。</param>
        /// <param name="endProgress">采样终止进度，范围 [0,1]。</param>
        /// <returns>当前扫描区间的采样点序列。</returns>
        private static List<Vector3> SamplePathPoints(
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float startProgress,
            float endProgress)
        {
            List<Vector3> sampledPoints = new List<Vector3>();
            if (flightPathSnapshot == null)
            {
                return sampledPoints;
            }

            float clampedStartProgress = Mathf.Clamp01(startProgress);
            float clampedEndProgress = Mathf.Clamp01(endProgress);
            if (clampedEndProgress < clampedStartProgress)
            {
                float swap = clampedStartProgress;
                clampedStartProgress = clampedEndProgress;
                clampedEndProgress = swap;
            }

            if (Mathf.Approximately(clampedStartProgress, clampedEndProgress))
            {
                sampledPoints.Add(NormalizeFlightPlanePoint(
                    ProjectileFlightPathUtility.EvaluatePosition(flightPathSnapshot, clampedEndProgress)));
                return sampledPoints;
            }

            int samplePointCount = ResolveSamplePointCount(flightPathSnapshot, clampedStartProgress, clampedEndProgress);
            for (int i = 0; i < samplePointCount; i++)
            {
                float stepProgress = samplePointCount == 1
                    ? clampedEndProgress
                    : Mathf.Lerp(clampedStartProgress, clampedEndProgress, (float)i / (samplePointCount - 1));
                Vector3 sampledPoint = ProjectileFlightPathUtility.EvaluatePosition(flightPathSnapshot, stepProgress);
                AppendSamplePoint(sampledPoints, NormalizeFlightPlanePoint(sampledPoint));
            }

            return sampledPoints;
        }

        /// <summary>
        /// 解析当前 flight path snapshot 在指定进度区间内所需的采样点数量。
        /// 线性路径维持两点即可，曲线路径按近似弧长转换成不大于固定步长的折线。
        /// </summary>
        /// <param name="flightPathSnapshot">当前段几何快照。</param>
        /// <param name="startProgress">采样起始进度。</param>
        /// <param name="endProgress">采样终止进度。</param>
        /// <returns>当前进度区间的采样点数量。</returns>
        private static int ResolveSamplePointCount(
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float startProgress,
            float endProgress)
        {
            if (flightPathSnapshot == null)
            {
                return 0;
            }

            if (flightPathSnapshot.Kind != ProjectileFlightPathKind.CubicBezier)
            {
                return 2;
            }

            float progressSpan = Mathf.Clamp01(endProgress) - Mathf.Clamp01(startProgress);
            float approximateLength = flightPathSnapshot.ApproximateLength > 0.001f
                ? flightPathSnapshot.ApproximateLength
                : ProjectileFlightPathUtility.EstimateLength(flightPathSnapshot);
            float sampledLength = Mathf.Max(0.001f, approximateLength * Mathf.Max(0.001f, progressSpan));
            int segmentCount = Mathf.Max(1, Mathf.CeilToInt(sampledLength / MaxCurveSampleStep));
            return Mathf.Max(3, segmentCount + 1);
        }

        /// <summary>
        /// 判断某个实体在当前第一版基础设施口径下，是否构成客观阻挡体。
        /// </summary>
        /// <param name="projectile">当前投射物宿主。</param>
        /// <param name="thing">待判定实体。</param>
        /// <returns>为 true 时表示该实体应阻断续段飞行。</returns>
        private static bool IsObjectiveBlocker(Projectile projectile, Thing thing)
        {
            if (projectile == null
                || thing == null
                || thing == projectile
                || !thing.Spawned)
            {
                return false;
            }

            if (thing is Pawn || thing is Plant)
            {
                return false;
            }

            if (thing is Building_Door door)
            {
                return !door.Open;
            }

            return thing.def != null && thing.def.Fillage == FillCategory.Full;
        }

        /// <summary>
        /// 生成首个客观阻挡体的简要审计文本。
        /// </summary>
        /// <param name="thing">当前阻挡体。</param>
        /// <returns>可直接写入日志的简要描述。</returns>
        private static string DescribeObjectiveBlocker(Thing thing)
        {
            if (thing == null)
            {
                return "none";
            }

            bool closedDoor = thing is Building_Door door && !door.Open;
            string fillage = thing.def != null
                ? thing.def.Fillage.ToString()
                : "<none>";
            return thing.ThingID
                + ":"
                + (thing.def != null ? thing.def.defName : thing.GetType().Name)
                + "{cell=" + thing.Position
                + ",fillage=" + fillage
                + ",closedDoor=" + closedDoor
                + "}";
        }

        /// <summary>
        /// 基于当前段的穿格事实，补齐首个客观阻挡的进入信息。
        /// 它只回答“第一处阻挡在哪里、路径大约何时触达”，不参与业务裁决。
        /// </summary>
        private static void TryPopulateFirstObjectiveBlockerFacts(
            Projectile projectile,
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float startProgress,
            float endProgress,
            List<IntVec3> traversedCells,
            SegmentCollisionRecord record)
        {
            if (projectile == null
                || projectile.Map == null
                || flightPathSnapshot == null
                || traversedCells == null
                || record == null)
            {
                return;
            }

            for (int i = 0; i < traversedCells.Count; i++)
            {
                IntVec3 cell = traversedCells[i];
                if (!TryResolveObjectiveBlockerInCell(projectile, cell, out Thing blocker, out string audit))
                {
                    continue;
                }

                record.CrossedObjectiveBlocker = true;
                record.FirstObjectiveBlockerCell = cell;
                record.FirstObjectiveBlockerThing = blocker;
                record.FirstObjectiveBlockerAudit = audit;
                record.FirstObjectiveBlockerProgress = ResolveFirstObjectiveBlockerProgress(
                    flightPathSnapshot,
                    startProgress,
                    endProgress,
                    cell);
                record.FirstObjectiveBlockerExactPosition = ResolveFirstObjectiveBlockerExactPosition(
                    flightPathSnapshot,
                    record.FirstObjectiveBlockerProgress,
                    cell);
                return;
            }
        }

        /// <summary>
        /// 判断指定格子里是否存在当前段需要承认的客观阻挡体。
        /// </summary>
        private static bool TryResolveObjectiveBlockerInCell(
            Projectile projectile,
            IntVec3 cell,
            out Thing blocker,
            out string audit)
        {
            blocker = null;
            audit = "none";

            Map map = projectile != null ? projectile.Map : null;
            if (map == null || !cell.InBounds(map))
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            if (things == null || things.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (!IsObjectiveBlocker(projectile, thing))
                {
                    continue;
                }

                blocker = thing;
                audit = DescribeObjectiveBlocker(thing);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 解析路径首次触达指定阻挡格的大致进度。
        /// 优先寻找“真实点已进入该格”的时刻；若只有 supercover 穿格事实，则退回到穿格边界逼近。
        /// </summary>
        private static float ResolveFirstObjectiveBlockerProgress(
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float startProgress,
            float endProgress,
            IntVec3 blockerCell)
        {
            float cellEntryProgress = TryResolveFirstCellEntryProgress(
                flightPathSnapshot,
                startProgress,
                endProgress,
                blockerCell);
            if (cellEntryProgress >= 0f)
            {
                return cellEntryProgress;
            }

            return ResolveFirstTraversedProgress(
                flightPathSnapshot,
                startProgress,
                endProgress,
                blockerCell);
        }

        /// <summary>
        /// 解析路径首次触达指定阻挡格的大致位置。
        /// 如果逼近位置已经落在阻挡格内，则直接采用；否则回退到格中心，保证宿主层锚点稳定。
        /// </summary>
        private static Vector3 ResolveFirstObjectiveBlockerExactPosition(
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float progress,
            IntVec3 blockerCell)
        {
            if (flightPathSnapshot == null)
            {
                return blockerCell.IsValid ? blockerCell.ToVector3Shifted() : Vector3.zero;
            }

            float clampedProgress = Mathf.Clamp01(progress);
            Vector3 evaluatedPosition = NormalizeFlightPlanePoint(
                ProjectileFlightPathUtility.EvaluatePosition(flightPathSnapshot, clampedProgress));
            return evaluatedPosition.ToIntVec3() == blockerCell
                ? evaluatedPosition
                : blockerCell.ToVector3Shifted();
        }

        /// <summary>
        /// 查找“路径真实采样点首次落入目标格”的大致进度。
        /// 若只发生擦角触格而没有采样点真正落入，则返回 -1 交由保守兜底处理。
        /// </summary>
        private static float TryResolveFirstCellEntryProgress(
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float startProgress,
            float endProgress,
            IntVec3 targetCell)
        {
            if (flightPathSnapshot == null)
            {
                return -1f;
            }

            Vector3 startPoint = NormalizeFlightPlanePoint(
                ProjectileFlightPathUtility.EvaluatePosition(flightPathSnapshot, startProgress));
            if (startPoint.ToIntVec3() == targetCell)
            {
                return startProgress;
            }

            int samplePointCount = ResolveSamplePointCount(flightPathSnapshot, startProgress, endProgress);
            float previousProgress = startProgress;
            for (int i = 1; i < samplePointCount; i++)
            {
                float currentProgress = samplePointCount == 1
                    ? endProgress
                    : Mathf.Lerp(startProgress, endProgress, (float)i / (samplePointCount - 1));
                Vector3 currentPoint = NormalizeFlightPlanePoint(
                    ProjectileFlightPathUtility.EvaluatePosition(flightPathSnapshot, currentProgress));
                if (currentPoint.ToIntVec3() == targetCell)
                {
                    return ResolveCellEntryProgressByBinarySearch(
                        flightPathSnapshot,
                        previousProgress,
                        currentProgress,
                        targetCell);
                }

                previousProgress = currentProgress;
            }

            return -1f;
        }

        /// <summary>
        /// 在已知终点已经落入目标格时，二分收束出更早的入格进度。
        /// </summary>
        private static float ResolveCellEntryProgressByBinarySearch(
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float startProgress,
            float endProgress,
            IntVec3 targetCell)
        {
            float low = startProgress;
            float high = endProgress;
            for (int i = 0; i < EntrySearchIterations; i++)
            {
                float mid = (low + high) * 0.5f;
                Vector3 midPoint = NormalizeFlightPlanePoint(
                    ProjectileFlightPathUtility.EvaluatePosition(flightPathSnapshot, mid));
                if (midPoint.ToIntVec3() == targetCell)
                {
                    high = mid;
                }
                else
                {
                    low = mid;
                }
            }

            return high;
        }

        /// <summary>
        /// 基于真实子区间穿格结果，保守逼近首次触达目标格的进度。
        /// 这个兜底主要服务于“只擦角但仍被 supercover 视为穿格”的场景。
        /// </summary>
        private static float ResolveFirstTraversedProgress(
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float startProgress,
            float endProgress,
            IntVec3 targetCell)
        {
            if (flightPathSnapshot == null)
            {
                return endProgress;
            }

            float low = startProgress;
            float high = endProgress;
            for (int i = 0; i < EntrySearchIterations; i++)
            {
                float mid = (low + high) * 0.5f;
                if (DoesSubPathTraverseCell(flightPathSnapshot, startProgress, mid, targetCell))
                {
                    high = mid;
                }
                else
                {
                    low = mid;
                }
            }

            return high;
        }

        /// <summary>
        /// 判断路径某个局部进度区间的真实穿格结果里，是否已经包含目标格。
        /// </summary>
        private static bool DoesSubPathTraverseCell(
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float startProgress,
            float endProgress,
            IntVec3 targetCell)
        {
            List<Vector3> sampledPathPoints = SamplePathPoints(flightPathSnapshot, startProgress, endProgress);
            List<IntVec3> traversedCells = EnumerateTraversedCells(sampledPathPoints);
            for (int i = 0; i < traversedCells.Count; i++)
            {
                if (traversedCells[i] == targetCell)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 把真实路径采样点序列拼接成宿主级穿格序列。
        /// 相邻采样点之间仍使用 supercover，保证折线化之后不漏掉角点附近的阻挡格。
        /// </summary>
        /// <param name="sampledPathPoints">按飞行顺序排列的采样点序列。</param>
        /// <returns>按飞行顺序排列的穿格序列。</returns>
        private static List<IntVec3> EnumerateTraversedCells(List<Vector3> sampledPathPoints)
        {
            List<IntVec3> cells = new List<IntVec3>();
            if (sampledPathPoints == null || sampledPathPoints.Count == 0)
            {
                return cells;
            }

            if (sampledPathPoints.Count == 1)
            {
                AppendTraversedCell(cells, sampledPathPoints[0].ToIntVec3());
                return cells;
            }

            for (int i = 1; i < sampledPathPoints.Count; i++)
            {
                List<IntVec3> partialCells = EnumerateTraversedCells(sampledPathPoints[i - 1], sampledPathPoints[i]);
                for (int j = 0; j < partialCells.Count; j++)
                {
                    AppendTraversedCell(cells, partialCells[j]);
                }
            }

            return cells;
        }

        /// <summary>
        /// 用二维 supercover 方式枚举线段穿过的所有格子。
        /// 这里宁可保守覆盖，也不遗漏角点附近的客观阻挡格。
        /// </summary>
        /// <param name="segmentStart">当前段起点。</param>
        /// <param name="segmentEnd">当前段终点。</param>
        /// <returns>按飞行方向排列的穿格序列。</returns>
        private static List<IntVec3> EnumerateTraversedCells(Vector3 segmentStart, Vector3 segmentEnd)
        {
            Vector3 start = NormalizeFlightPlanePoint(segmentStart);
            Vector3 end = NormalizeFlightPlanePoint(segmentEnd);
            IntVec3 startCell = start.ToIntVec3();
            IntVec3 endCell = end.ToIntVec3();
            List<IntVec3> cells = new List<IntVec3>();
            AppendTraversedCell(cells, startCell);

            if (startCell == endCell)
            {
                return cells;
            }

            float dx = end.x - start.x;
            float dz = end.z - start.z;
            int stepX = dx > 0f ? 1 : dx < 0f ? -1 : 0;
            int stepZ = dz > 0f ? 1 : dz < 0f ? -1 : 0;
            float tDeltaX = stepX != 0 ? 1f / Mathf.Abs(dx) : float.PositiveInfinity;
            float tDeltaZ = stepZ != 0 ? 1f / Mathf.Abs(dz) : float.PositiveInfinity;
            float nextBoundaryX = stepX > 0 ? startCell.x + 1f : startCell.x;
            float nextBoundaryZ = stepZ > 0 ? startCell.z + 1f : startCell.z;
            float tMaxX = stepX != 0
                ? Mathf.Abs((nextBoundaryX - start.x) / dx)
                : float.PositiveInfinity;
            float tMaxZ = stepZ != 0
                ? Mathf.Abs((nextBoundaryZ - start.z) / dz)
                : float.PositiveInfinity;
            int currentX = startCell.x;
            int currentZ = startCell.z;

            while (currentX != endCell.x || currentZ != endCell.z)
            {
                if (Mathf.Approximately(tMaxX, tMaxZ))
                {
                    if (stepX != 0)
                    {
                        AppendTraversedCell(cells, new IntVec3(currentX + stepX, 0, currentZ));
                    }

                    if (stepZ != 0)
                    {
                        AppendTraversedCell(cells, new IntVec3(currentX, 0, currentZ + stepZ));
                    }

                    currentX += stepX;
                    currentZ += stepZ;
                    tMaxX += tDeltaX;
                    tMaxZ += tDeltaZ;
                }
                else if (tMaxX < tMaxZ)
                {
                    currentX += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    currentZ += stepZ;
                    tMaxZ += tDeltaZ;
                }

                AppendTraversedCell(cells, new IntVec3(currentX, 0, currentZ));
            }

            return cells;
        }

        /// <summary>
        /// 只在序列末尾追加新的格子，避免重复记录同一个穿格。
        /// </summary>
        /// <param name="cells">目标格子序列。</param>
        /// <param name="cell">当前待追加格子。</param>
        private static void AppendTraversedCell(List<IntVec3> cells, IntVec3 cell)
        {
            if (cells.Count == 0 || cells[cells.Count - 1] != cell)
            {
                cells.Add(cell);
            }
        }

        /// <summary>
        /// 仅在当前采样点与上一个采样点不重合时追加，避免曲线采样端点重复。
        /// </summary>
        /// <param name="sampledPoints">采样点序列。</param>
        /// <param name="sampledPoint">当前采样点。</param>
        private static void AppendSamplePoint(List<Vector3> sampledPoints, Vector3 sampledPoint)
        {
            if (sampledPoints.Count == 0
                || (sampledPoints[sampledPoints.Count - 1] - sampledPoint).sqrMagnitude > 0.0001f)
            {
                sampledPoints.Add(sampledPoint);
            }
        }

        /// <summary>
        /// 把单个坐标点压回原版地图平面，确保段扫描在同一高度平面进行。
        /// </summary>
        /// <param name="point">待归一的坐标点。</param>
        /// <returns>高度归一后的平面点。</returns>
        private static Vector3 NormalizeFlightPlanePoint(Vector3 point)
        {
            point.y = 0f;
            return point;
        }
    }
}
