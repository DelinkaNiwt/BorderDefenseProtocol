using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 追踪弹目标搜索工具——从弹道当前位置搜索最近的有效敌方目标。
    /// 与TrackingModule解耦，可独立复用。
    /// </summary>
    public static class TargetSearcher
    {
        /// <summary>
        /// 搜索最近的有效敌方目标。
        /// </summary>
        /// <param name="map">当前地图</param>
        /// <param name="position">搜索中心位置</param>
        /// <param name="radius">搜索半径（格）</param>
        /// <param name="launcher">发射者（用于判断敌我）</param>
        /// <returns>最近的有效目标，无则返回LocalTargetInfo.Invalid</returns>
        public static LocalTargetInfo FindNearestEnemy(
            Map map, IntVec3 position, float radius, Thing launcher)
        {
            if (map == null || launcher == null)
                return LocalTargetInfo.Invalid;

            Faction launcherFaction = launcher.Faction;
            if (launcherFaction == null)
                return LocalTargetInfo.Invalid;

            float bestDistSq = radius * radius;
            Pawn bestTarget = null;

            // 遍历地图上所有已生成的Pawn
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                // 跳过：死亡、倒地、友方、中立
                if (p.Dead || p.Downed) continue;
                if (p.Faction == null) continue;
                if (!p.Faction.HostileTo(launcherFaction)) continue;

                float distSq = (p.Position - position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestTarget = p;
                }
            }

            return bestTarget != null
                ? new LocalTargetInfo(bestTarget)
                : LocalTargetInfo.Invalid;
        }
    }
}
