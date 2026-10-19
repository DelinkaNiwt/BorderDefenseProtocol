using System.Collections.Generic;
using Verse.AI;
using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 浮空安全降落的原版合法格查询工具。
    /// </summary>
    public static class FloatingFlightLandingUtility
    {
        /// <summary>判断当前格是否符合原版地面站立的基础通行规则。</summary>
        public static bool IsGroundLegal(Pawn pawn, IntVec3 cell)
        {
            return pawn != null
                && pawn.Map != null
                && cell.InBounds(pawn.Map)
                && pawn.Map.pathing.Normal.pathGrid.WalkableFast(cell);
        }

        /// <summary>沿原版当前路径寻找第一个地面合法格。</summary>
        public static bool TryFindForwardLegalCell(Pawn pawn, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            Pawn_PathFollower pather = pawn?.pather;
            PawnPath path = pather?.curPath;
            if (pather == null || !pather.Moving || path == null || !path.Found)
            {
                return false;
            }

            for (int index = 0; index < path.NodesLeftCount; index++)
            {
                IntVec3 candidate = path.Peek(index);
                if (IsGroundLegal(pawn, candidate))
                {
                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>在当前 Pawn 周围按原版径向顺序寻找最近合法格。</summary>
        public static bool TryFindNearestLegalCell(Pawn pawn, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (pawn == null || pawn.Map == null)
            {
                return false;
            }

            IReadOnlyList<IntVec3> radialPattern = GenRadial.RadialPattern;
            for (int index = 0; index < radialPattern.Count; index++)
            {
                IntVec3 candidate = pawn.Position + radialPattern[index];
                if (IsGroundLegal(pawn, candidate))
                {
                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 读取当前路径跟随器的原版目标，供恢复路径插入时保留 Job 目标语义。
        /// </summary>
        public static LocalTargetInfo GetOriginalDestination(Pawn pawn)
        {
            return pawn?.pather?.Destination ?? LocalTargetInfo.Invalid;
        }
    }
}
