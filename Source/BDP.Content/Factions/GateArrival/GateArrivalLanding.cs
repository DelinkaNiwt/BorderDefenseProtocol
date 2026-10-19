using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>门抵达的可选落点样式；由各自抵达方式上的 BDP 扩展明确选择。</summary>
    public enum GateArrivalLandingPattern
    {
        /// <summary>沿用原版边缘步行入场的落点中心。</summary>
        Edge,
        /// <summary>将门分配到地图的不同边缘。</summary>
        MultiSide,
        /// <summary>利用原版避险网格寻找防卫火力线朝殖民地内侧的点位。</summary>
        BreachLine
    }

    /// <summary>根据抵达方式配置选择合法门点；本类不决定袭击策略、目标或战斗行为。</summary>
    public static class GateArrivalLanding
    {
        /// <summary>按落点样式生成候选门点；Edge 保持原有的边缘中心算法。</summary>
        public static List<IntVec3> FindPositions(Map map, int count, IntVec3 edgeCenter,
            GateArrivalSettings settings, GateArrivalModeExtension arrivalMode)
        {
            if (map == null || count <= 0) return new List<IntVec3>();
            GateArrivalSettings safeSettings = (settings ?? new GateArrivalSettings()).Snapshot();
            GateArrivalModeExtension safeMode = (arrivalMode ?? new GateArrivalModeExtension()).Snapshot();
            switch (safeMode.landingPattern)
            {
                case GateArrivalLandingPattern.MultiSide:
                    return FindMultiSidePositions(map, count, safeSettings, safeMode.minimumGateSpacing);
                case GateArrivalLandingPattern.BreachLine:
                    return FindBreachLinePositions(map, count, safeMode);
                default:
                    return FindEdgePositions(map, count, edgeCenter, safeSettings.placementRadius);
            }
        }

        /// <summary>完全沿用先前版本：在原版选出的边缘中心附近随机找互不重复的可行走格。</summary>
        public static List<IntVec3> FindEdgePositions(Map map, int count, IntVec3 edgeCenter, int radius)
        {
            var positions = new List<IntVec3>();
            if (map == null || count <= 0) return positions;
            for (int i = 0; i < count; i++)
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    IntVec3 cell = CellFinder.RandomClosewalkCellNear(edgeCenter, map, Math.Max(1, radius));
                    if (!Thing_GateArrival.IsValidDestination(map, cell) || positions.Contains(cell)) continue;
                    positions.Add(cell);
                    break;
                }
            return positions;
        }

        /// <summary>按门数均匀分配四条地图边，并在各自边缘附近寻找合法站立点。</summary>
        public static List<IntVec3> FindMultiSidePositions(Map map, int count, GateArrivalSettings settings, int minimumSpacing)
        {
            var anchors = new List<IntVec3>();
            if (map == null || count <= 0) return anchors;
            int startSide = Rand.Range(0, 4);
            for (int i = 0; i < count; i++)
            {
                int side = SideForGate(i, count, startSide);
                int x = side < 2 ? Rand.Range(1, Math.Max(2, map.Size.x - 1)) : (side == 2 ? 0 : map.Size.x - 1);
                int z = side >= 2 ? Rand.Range(1, Math.Max(2, map.Size.z - 1)) : (side == 0 ? 0 : map.Size.z - 1);
                anchors.Add(new IntVec3(x, 0, z));
            }
            var candidates = new List<IntVec3>();
            foreach (IntVec3 anchor in anchors)
            {
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    IntVec3 cell = CellFinder.RandomClosewalkCellNear(anchor, map, Math.Max(1, settings.placementRadius));
                    if (!Thing_GateArrival.IsValidDestination(map, cell) || candidates.Contains(cell)) continue;
                    candidates.Add(cell);
                    break;
                }
            }
            return SelectSpreadPositions(map, candidates, count, minimumSpacing);
        }

        /// <summary>把各门近似等距地分配到四条边，门数超过四时允许同边多个点。</summary>
        public static int SideForGate(int gateIndex, int count, int startSide)
        {
            if (count <= 0) return 0;
            return (startSide + (int)Math.Round((double)gateIndex * 4 / count)) % 4;
        }

        /// <summary>以原版 AvoidGrid 为炮塔火力范围近似图，挑选高危险格朝基地一侧的安全邻格。</summary>
        public static List<IntVec3> FindBreachLinePositions(Map map, int count, GateArrivalModeExtension arrivalMode)
        {
            var positions = new List<IntVec3>();
            if (map == null || count <= 0 || map.avoidGrid == null) return positions;
            GateArrivalModeExtension safeMode = (arrivalMode ?? new GateArrivalModeExtension()).Snapshot();
            IntVec3 center = EstimateColonyCenter(map);
            int width = map.Size.x;
            int height = map.Size.z;
            int gridLength = map.avoidGrid.Grid.Length;
            if (gridLength != map.cellIndices.NumGridCells) return positions;
            var danger = new byte[width * height];
            for (int z = 0; z < height; z++)
                for (int x = 0; x < width; x++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    danger[z * width + x] = map.avoidGrid[cell];
                }
            List<IntVec3> candidates = CollectBreachLineCandidates(width, height, center, danger,
                safeMode.minimumDanger, safeMode.colonyCoreBuffer)
                .Where(cell => Thing_GateArrival.IsValidDestination(map, cell))
                .ToList();
            return SelectSpreadPositions(map, candidates, count, safeMode.minimumGateSpacing);
        }

        /// <summary>从合成或原版避险网格中提取火力线内侧候选；数组按每行宽度排列。</summary>
        public static List<IntVec3> CollectBreachLineCandidates(int width, int height, IntVec3 colonyCenter,
            byte[] dangerGrid, int minimumDanger, int colonyCoreBuffer)
        {
            var candidates = new HashSet<IntVec3>();
            if (width < 3 || height < 3 || dangerGrid == null || dangerGrid.Length != width * height) return candidates.ToList();
            int threshold = Math.Max(1, Math.Min(255, minimumDanger));
            int coreBuffer = Math.Max(0, Math.Min(256, colonyCoreBuffer));
            int coreBufferSquared = coreBuffer * coreBuffer;
            for (int z = 1; z < height - 1; z++)
                for (int x = 1; x < width - 1; x++)
                {
                    IntVec3 threatCell = new IntVec3(x, 0, z);
                    if (dangerGrid[z * width + x] < threshold) continue;
                    int threatDistance = threatCell.DistanceToSquared(colonyCenter);
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            int innerX = x + dx;
                            int innerZ = z + dz;
                            int innerIndex = innerZ * width + innerX;
                            if (dangerGrid[innerIndex] >= threshold) continue;
                            IntVec3 innerCell = new IntVec3(innerX, 0, innerZ);
                            int innerDistance = innerCell.DistanceToSquared(colonyCenter);
                            if (innerDistance >= threatDistance || innerDistance < coreBufferSquared) continue;
                            candidates.Add(innerCell);
                        }
                }
            return candidates.OrderBy(cell => cell.x).ThenBy(cell => cell.z).ToList();
        }

        /// <summary>优先以最大玩家卧室或营房的中心估算内侧；找不到时退回建筑群平均中心。</summary>
        public static IntVec3 EstimateColonyCenter(Map map)
        {
            // 原版 AllRooms 直接返回缓存列表，查询前先处理待重建的房间网格。
            map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
            Room residential = map.regionGrid.AllRooms
                .Where(room => room != null && room.ProperRoom && !room.PsychologicallyOutdoors
                    && !room.TouchesMapEdge && !room.IsHuge && !room.IsPrisonCell
                    && (room.Role == RoomRoleDefOf.Bedroom || room.Role == RoomRoleDefOf.Barracks)
                    && room.ContainedBeds.Any(bed => bed.Faction == Faction.OfPlayer))
                .OrderByDescending(room => room.CellCount)
                .FirstOrDefault();
            if (residential != null)
            {
                long roomX = 0;
                long roomZ = 0;
                int roomCellCount = 0;
                foreach (IntVec3 cell in residential.Cells)
                {
                    roomX += cell.x;
                    roomZ += cell.z;
                    roomCellCount++;
                }
                if (roomCellCount > 0) return new IntVec3((int)(roomX / roomCellCount), 0, (int)(roomZ / roomCellCount));
            }
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            if (buildings.NullOrEmpty()) return new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
            long sumX = 0;
            long sumZ = 0;
            foreach (Building building in buildings)
            {
                sumX += building.Position.x;
                sumZ += building.Position.z;
            }
            return new IntVec3((int)(sumX / buildings.Count), 0, (int)(sumZ / buildings.Count));
        }

        /// <summary>合法候选之间尽量留出间隔并分散取点；空间不足时逐步放宽间距。</summary>
        public static List<IntVec3> SelectSpreadPositions(Map map, IEnumerable<IntVec3> source, int count, int minimumSpacing)
        {
            var remaining = source.Where(cell => Thing_GateArrival.IsValidDestination(map, cell)).Distinct().ToList();
            var selected = new List<IntVec3>();
            int spacingSquared = Math.Max(0, minimumSpacing) * Math.Max(0, minimumSpacing);
            while (selected.Count < count && remaining.Count > 0)
            {
                List<IntVec3> eligible = remaining.Where(cell => selected.All(other =>
                    cell.DistanceToSquared(other) >= spacingSquared)).ToList();
                if (eligible.Count == 0 && spacingSquared > 0)
                {
                    spacingSquared /= 4;
                    continue;
                }
                if (eligible.Count == 0) break;
                IntVec3 chosen = selected.Count == 0 ? eligible[Rand.Range(0, eligible.Count)]
                    : FarthestCandidate(eligible, selected);
                selected.Add(chosen);
                remaining.Remove(chosen);
            }
            return selected;
        }

        /// <summary>在满足最低间距的候选中随机打破平局，优先扩散到最远位置。</summary>
        private static IntVec3 FarthestCandidate(List<IntVec3> candidates, List<IntVec3> selected)
        {
            int bestDistance = -1;
            var best = new List<IntVec3>();
            foreach (IntVec3 candidate in candidates)
            {
                int nearest = selected.Min(cell => candidate.DistanceToSquared(cell));
                if (nearest > bestDistance) { bestDistance = nearest; best.Clear(); best.Add(candidate); }
                else if (nearest == bestDistance) best.Add(candidate);
            }
            return best[Rand.Range(0, best.Count)];
        }
    }
}
