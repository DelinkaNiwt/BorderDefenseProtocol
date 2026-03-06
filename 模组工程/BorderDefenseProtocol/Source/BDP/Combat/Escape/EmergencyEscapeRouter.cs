using System.Linq;
using BDP.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Combat
{
    /// <summary>
    /// 紧急脱离传送目标路由器。
    /// 实现三级回退规则：信标建筑 → 殖民者区域 → 随机安全位置。
    /// </summary>
    public static class EmergencyEscapeRouter
    {
        /// <summary>
        /// 查找传送目标位置。
        /// </summary>
        /// <param name="pawn">需要传送的Pawn</param>
        /// <param name="map">当前地图</param>
        /// <returns>传送目标位置，如果找不到返回IntVec3.Invalid</returns>
        public static IntVec3 FindEscapeDestination(Pawn pawn, Map map)
        {
            // 第一优先级：紧急脱离信标
            IntVec3 beaconPos = TryFindBeaconPosition(map);
            if (beaconPos.IsValid)
            {
                return beaconPos;
            }

            // 第二优先级：殖民者建筑附近
            IntVec3 colonistAreaPos = TryFindColonistAreaPosition(pawn, map);
            if (colonistAreaPos.IsValid)
            {
                return colonistAreaPos;
            }

            // 第三优先级：随机安全位置
            IntVec3 randomSafePos = TryFindRandomSafePosition(pawn, map);
            if (randomSafePos.IsValid)
            {
                return randomSafePos;
            }

            // 兜底：原地
            Log.Warning($"[BDP] 紧急脱离无法找到安全位置，{pawn.Name}将在原地释放");
            return pawn.Position;
        }

        /// <summary>
        /// 尝试查找紧急脱离信标位置。
        /// </summary>
        private static IntVec3 TryFindBeaconPosition(Map map)
        {
            var beacons = map.listerBuildings.AllBuildingsColonistOfDef(BDP_DefOf.BDP_EmergencyBeacon);

            // 查找通电且可用的信标
            foreach (var beacon in beacons)
            {
                var powerComp = beacon.TryGetComp<CompPowerTrader>();
                if (powerComp == null || powerComp.PowerOn)
                {
                    // 在信标周围查找可站立位置（紧邻1格范围）
                    if (CellFinder.TryFindRandomCellNear(
                        beacon.Position,
                        map,
                        1,
                        (IntVec3 c) => c.Standable(map) && !c.Fogged(map),
                        out IntVec3 result))
                    {
                        return result;
                    }
                }
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// 尝试查找殖民者建筑附近位置。
        /// </summary>
        private static IntVec3 TryFindColonistAreaPosition(Pawn pawn, Map map)
        {
            var colonistBuildings = map.listerBuildings.allBuildingsColonist;
            if (colonistBuildings.Count == 0)
            {
                return IntVec3.Invalid;
            }

            // 选择最近的殖民者建筑
            var nearestBuilding = colonistBuildings
                .OrderBy(b => b.Position.DistanceTo(pawn.Position))
                .FirstOrDefault();

            if (nearestBuilding != null)
            {
                // 在建筑周围查找可站立位置
                if (CellFinder.TryFindRandomCellNear(
                    nearestBuilding.Position,
                    map,
                    10,
                    (IntVec3 c) => c.Standable(map) && !c.Fogged(map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly),
                    out IntVec3 result))
                {
                    return result;
                }
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// 尝试查找随机安全位置。
        /// 参考轨道贸易空投的回退逻辑。
        /// </summary>
        private static IntVec3 TryFindRandomSafePosition(Pawn pawn, Map map)
        {
            // 使用RimWorld的CellFinder查找可站立的随机位置
            if (CellFinder.TryFindRandomCellNear(
                map.Center,
                map,
                map.Size.x / 2,
                (IntVec3 c) => c.Standable(map) && !c.Fogged(map) && !c.Roofed(map),
                out IntVec3 result))
            {
                return result;
            }

            return IntVec3.Invalid;
        }
    }
}
