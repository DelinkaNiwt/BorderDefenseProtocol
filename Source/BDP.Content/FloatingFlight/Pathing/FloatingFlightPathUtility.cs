using BDP.Content.FloatingFlight;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Pathing
{
    /// <summary>
    /// 浮空路径种类、上下文和露天空中代价的集中工具。
    /// </summary>
    public static class FloatingFlightPathUtility
    {
        /// <summary>解析 Pawn 当前路径应使用的网格种类。</summary>
        public static FloatingFlightPathKind ResolvePathKind(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
            {
                return FloatingFlightPathKind.Vanilla;
            }

            if (FloatingFlightUtility.IsActuallyFlying(pawn)
                && FloatingFlightLedgerService.TryGetRecord(
                    pawn,
                    out FloatingFlightRecord record)
                && record.PendingSafeLanding)
            {
                return FloatingFlightPathKind.Hybrid;
            }

            FloatingFlightMode mode = FloatingFlightModeResolver.ResolveEffectiveMode(pawn);
            if (mode == FloatingFlightMode.MoveOnly)
            {
                return FloatingFlightPathKind.Hybrid;
            }

            if (mode == FloatingFlightMode.Always)
            {
                return pawn.Position.Roofed(pawn.Map)
                    ? FloatingFlightPathKind.Hybrid
                    : FloatingFlightPathKind.Always;
            }

            // 外部直接添加 Hediff 时仍允许它沿露天路径安全结束，而不改变偏好真值。
            return FloatingFlightUtility.IsActuallyFlying(pawn)
                && !pawn.Position.Roofed(pawn.Map)
                ? FloatingFlightPathKind.Hybrid
                : FloatingFlightPathKind.Vanilla;
        }

        /// <summary>按路径种类读取当前地图对应的正式 PathingContext。</summary>
        public static PathingContext GetContext(Map map, FloatingFlightPathKind kind)
        {
            if (map == null)
            {
                return null;
            }

            if (kind == FloatingFlightPathKind.Hybrid)
            {
                return map.pathing.Get(FloatingFlightDefOf.BDP_PathGrid_FloatingHybrid);
            }

            if (kind == FloatingFlightPathKind.Always)
            {
                return map.pathing.Get(FloatingFlightDefOf.BDP_PathGrid_FloatingAlways);
            }

            return map.pathing.Normal;
        }

        /// <summary>判断 PathingContext 是否为 BDP 浮空上下文。</summary>
        public static bool IsFloatingContext(PathingContext context)
        {
            return context != null
                && (context.pathGrid?.def == FloatingFlightDefOf.BDP_PathGrid_FloatingHybrid
                    || context.pathGrid?.def == FloatingFlightDefOf.BDP_PathGrid_FloatingAlways);
        }

        /// <summary>
        /// 判断浮空路径的最终停留格。
        /// 中间路径使用浮空网格；最终停留仍遵循原版可站立性，始终浮空额外排除屋顶。
        /// </summary>
        public static bool IsLegalLandingCell(
            Map map,
            IntVec3 cell,
            FloatingFlightPathKind kind)
        {
            if (map == null || !cell.InBounds(map))
            {
                return false;
            }

            if (kind == FloatingFlightPathKind.Always && cell.Roofed(map))
            {
                return false;
            }

            return map.pathing.Normal.pathGrid.WalkableFast(cell);
        }

        /// <summary>计算露天空中格的最小路径代价，只保留火焰避让。</summary>
        public static int CalculateOpenAirCost(
            Map map,
            IntVec3 cell,
            bool perceivedStatic)
        {
            if (map == null || !cell.InBounds(map))
            {
                return PathGrid.ImpassableCost;
            }

            int cost = 0;
            if (!perceivedStatic)
            {
                return cost;
            }

            Fire fire = FindFire(map, cell);
            if (fire != null && fire.parent == null)
            {
                cost += 1000;
            }

            for (int index = 0; index < 8; index++)
            {
                IntVec3 adjacent = cell + GenAdj.AdjacentCells[index];
                if (!adjacent.InBounds(map))
                {
                    continue;
                }

                fire = FindFire(map, adjacent);
                if (fire != null && fire.parent == null)
                {
                    cost += 150;
                }
            }

            return cost;
        }

        /// <summary>从指定格的物件列表中查找火焰。</summary>
        private static Fire FindFire(Map map, IntVec3 cell)
        {
            System.Collections.Generic.List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            for (int index = 0; index < things.Count; index++)
            {
                if (things[index] is Fire fire)
                {
                    return fire;
                }
            }

            return null;
        }
    }
}
