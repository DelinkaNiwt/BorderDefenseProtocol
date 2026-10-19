using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.GroundDash
{
    /// <summary>一次地面冲刺目标解析的结果。</summary>
    public sealed class GroundDashPathResult
    {
        /// <summary>冲刺起点。</summary>
        public IntVec3 StartCell;

        /// <summary>玩家请求的目标格。</summary>
        public IntVec3 RequestedCell;

        /// <summary>按最大距离裁剪后的请求格。</summary>
        public IntVec3 ClampedTargetCell;

        /// <summary>实际允许到达的最后一格。</summary>
        public IntVec3 EndCell;

        /// <summary>路径是否被地图几何物截断。</summary>
        public bool BlockedByGeometry;

        /// <summary>玩家请求的目标是否在最大射程内。</summary>
        public bool WithinRange;

        /// <summary>裁剪后的请求格是否完整可达。</summary>
        public bool ReachedRequestedCell;

        /// <summary>是否存在至少一格有效移动。</summary>
        public bool HasMovement => EndCell.IsValid && EndCell != StartCell;
    }

    /// <summary>
    /// 地面冲刺共享工具。
    ///
    /// 这里集中处理目标路径、地图几何阻挡、飞行器生成和选中状态恢复；
    /// 不读取 Pawn 体型，也不放入蚱蜢或光魂推进的业务规则。
    /// </summary>
    public static class GroundDashUtility
    {
        /// <summary>
        /// 解析一条不绕路的地面冲刺路径。
        /// 路径遇到不可通行格时停在阻挡前最后一个可用格。
        /// </summary>
        public static bool TryResolvePath(
            Map map,
            IntVec3 startCell,
            IntVec3 requestedCell,
            float maxRange,
            out GroundDashPathResult result)
        {
            result = new GroundDashPathResult
            {
                StartCell = startCell,
                RequestedCell = requestedCell,
                ClampedTargetCell = IntVec3.Invalid,
                EndCell = IntVec3.Invalid
            };

            if (map == null || !startCell.IsValid || !startCell.InBounds(map)
                || !requestedCell.IsValid || !requestedCell.InBounds(map)
                || maxRange <= 0f)
            {
                return false;
            }

            IntVec3 clampedTarget = ClampToRange(startCell, requestedCell, maxRange);
            result.ClampedTargetCell = clampedTarget;
            result.WithinRange = startCell.DistanceTo(requestedCell) <= maxRange;

            List<IntVec3> cells = GenSight.BresenhamCellsBetween(startCell, clampedTarget);
            IntVec3 lastReachableCell = startCell;
            for (int i = 1; i < cells.Count; i++)
            {
                IntVec3 cell = cells[i];
                if (IsPathBlockingCell(map, cell))
                {
                    result.BlockedByGeometry = true;
                    break;
                }

                lastReachableCell = cell;
            }

            result.EndCell = lastReachableCell;
            result.ReachedRequestedCell = !result.BlockedByGeometry
                && result.EndCell == clampedTarget
                && IsValidDashCell(map, result.EndCell);
            return result.HasMovement && IsValidDashCell(map, result.EndCell);
        }

        /// <summary>判断地面冲刺是否能完整到达请求格。</summary>
        public static bool CanReachRequestedCell(
            Map map,
            IntVec3 startCell,
            IntVec3 requestedCell,
            float maxRange)
        {
            GroundDashPathResult result;
            if (!TryResolvePath(map, startCell, requestedCell, maxRange, out result))
            {
                return false;
            }
            return result.WithinRange && result.ReachedRequestedCell;
        }

        /// <summary>
        /// 判断目标格是否是地面冲刺可用格。
        /// 预览与落点都使用这一规则，不把 Pawn 当成地图阻挡。
        /// </summary>
        public static bool IsValidDashCell(Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }
            if (cell.Fogged(map) || cell.Impassable(map) || !cell.Walkable(map))
            {
                return false;
            }

            Building building = cell.GetEdifice(map);
            Building_Door door = building as Building_Door;
            return door == null || door.Open;
        }

        /// <summary>判断地图几何是否阻断路径，不读取 Pawn 占用和体型。</summary>
        public static bool IsPathBlockingCell(Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return true;
            }
            if (cell.Impassable(map) || !cell.Walkable(map))
            {
                return true;
            }

            Building building = cell.GetEdifice(map);
            Building_Door door = building as Building_Door;
            return door != null && !door.Open;
        }

        /// <summary>
        /// 生成并发射一次自定义地面冲刺飞行器。
        /// PawnFlyer.MakeFlyer 会暂时移除 Pawn，因此先保存地图和选中状态。
        /// </summary>
        public static bool DoDash(
            Pawn pawn,
            IntVec3 targetCell,
            VerbProperties verbProps,
            ThingDef pawnFlyerDef)
        {
            return DoDash(pawn, targetCell, verbProps, pawnFlyerDef, null);
        }

        /// <summary>
        /// 生成并发射地面冲刺飞行器，并在生成后、入图前执行业务初始化。
        /// </summary>
        public static bool DoDash(
            Pawn pawn,
            IntVec3 targetCell,
            VerbProperties verbProps,
            ThingDef pawnFlyerDef,
            Action<PawnFlyer> configureFlyer)
        {
            if (pawn == null || pawn.Map == null || !targetCell.IsValid || pawnFlyerDef == null)
            {
                return false;
            }

            Map map = pawn.Map;
            IntVec3 startCell = pawn.Position;
            Vector3 direction = (targetCell - startCell).ToVector3();
            direction.Normalize();
            bool wasSelected = Find.Selector.IsSelected(pawn);

            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                pawnFlyerDef,
                pawn,
                targetCell,
                verbProps != null ? verbProps.flightEffecterDef : null,
                verbProps != null ? verbProps.soundLanding : null,
                verbProps != null && verbProps.flyWithCarriedThing,
                null);
            if (flyer == null)
            {
                return false;
            }

            if (configureFlyer != null)
            {
                configureFlyer(flyer);
            }

            FleckMaker.ThrowDustPuff(startCell.ToVector3Shifted() - direction, map, 2f);
            GenSpawn.Spawn(flyer, targetCell, map);
            if (wasSelected)
            {
                Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
            }
            return true;
        }

        /// <summary>把目标格限制在最大射程内。</summary>
        private static IntVec3 ClampToRange(IntVec3 startCell, IntVec3 requestedCell, float maxRange)
        {
            if (startCell.DistanceTo(requestedCell) <= maxRange)
            {
                return requestedCell;
            }

            Vector3 direction = (requestedCell - startCell).ToVector3();
            direction.Normalize();
            return (startCell.ToVector3() + direction * maxRange).ToIntVec3();
        }
    }
}
