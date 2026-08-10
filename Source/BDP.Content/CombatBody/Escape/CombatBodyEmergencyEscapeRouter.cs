using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 紧急脱离落点路由器。
    /// 它只负责选一个可落脚位置，不承担传送或业务收尾。
    /// </summary>
    internal static class CombatBodyEmergencyEscapeRouter
    {
        /// <summary>
        /// 为当前 Pawn 解析一个紧急脱离落点。
        /// 路由只负责落点选择，按信标、殖民者建筑、原地附近、地图安全格逐层回退。
        /// </summary>
        internal static IntVec3 FindEscapeDestination(Pawn pawn, Map map)
        {
            if (pawn == null || map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 destination;
            if (TryFindBeaconDestination(map, out destination))
            {
                return destination;
            }

            if (TryFindColonistAreaDestination(pawn, map, out destination))
            {
                return destination;
            }

            if (TryFindLocalSafeDestination(pawn, map, out destination))
            {
                return destination;
            }

            if (TryFindMapSafeDestination(map, out destination))
            {
                return destination;
            }

            return pawn.Position;
        }

        /// <summary>
        /// 优先查找已启动的紧急脱离信标附近落点。
        /// </summary>
        private static bool TryFindBeaconDestination(Map map, out IntVec3 destination)
        {
            destination = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            foreach (Building_EmergencyEscapeBeacon beacon in map.listerBuildings.AllBuildingsColonistOfClass<Building_EmergencyEscapeBeacon>())
            {
                if (beacon == null || !beacon.IsActiveAnchor)
                {
                    continue;
                }

                if (TryFindStandableNear(beacon.Position, map, 1, false, out destination))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 查找最近殖民者建筑附近的安全落点。
        /// </summary>
        private static bool TryFindColonistAreaDestination(Pawn pawn, Map map, out IntVec3 destination)
        {
            destination = IntVec3.Invalid;
            if (pawn == null || map == null || map.listerBuildings.allBuildingsColonist.Count == 0)
            {
                return false;
            }

            Building nearestBuilding = null;
            float nearestDistance = float.MaxValue;
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building == null || building.Destroyed || !building.Spawned)
                {
                    continue;
                }

                float distance = building.Position.DistanceToSquared(pawn.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestBuilding = building;
                }
            }

            return nearestBuilding != null
                && TryFindStandableNear(nearestBuilding.Position, map, 10, false, out destination);
        }

        /// <summary>
        /// 保留当前小人附近的短距离安全回退。
        /// </summary>
        private static bool TryFindLocalSafeDestination(Pawn pawn, Map map, out IntVec3 destination)
        {
            destination = IntVec3.Invalid;
            return pawn != null
                && TryFindStandableNear(pawn.Position, map, 12, false, out destination);
        }

        /// <summary>
        /// 查找地图中心附近的无顶安全格，作为旧 BDP 风格的大范围回退。
        /// </summary>
        private static bool TryFindMapSafeDestination(Map map, out IntVec3 destination)
        {
            destination = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            return CellFinder.TryFindRandomCellNear(
                map.Center,
                map,
                map.Size.x / 2,
                cell => cell.IsValid
                    && cell.InBounds(map)
                    && cell.Standable(map)
                    && !cell.Fogged(map)
                    && !cell.Roofed(map),
                out destination);
        }

        /// <summary>
        /// 在指定中心附近查找可站立、未迷雾的落点。
        /// </summary>
        private static bool TryFindStandableNear(IntVec3 center, Map map, int radius, bool requireUnroofed, out IntVec3 destination)
        {
            destination = IntVec3.Invalid;
            if (map == null || !center.IsValid)
            {
                return false;
            }

            return CellFinder.TryFindRandomCellNear(
                center,
                map,
                radius,
                cell => cell.IsValid
                    && cell.InBounds(map)
                    && cell.Standable(map)
                    && !cell.Fogged(map)
                    && (!requireUnroofed || !cell.Roofed(map)),
                out destination);
        }
    }
}
