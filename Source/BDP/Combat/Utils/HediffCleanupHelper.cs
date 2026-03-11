using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// Hediff批量操作工具类。
    /// 提供安全的Hediff批量移除和部位检查方法。
    /// </summary>
    public static class HediffCleanupHelper
    {
        /// <summary>
        /// 按条件批量移除Hediff。
        /// 使用二阶段模式(收集→移除)避免遍历中修改集合的问题。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="predicate">移除条件,返回true的Hediff将被移除</param>
        public static void RemoveHediffsWhere(Pawn pawn, Predicate<Hediff> predicate)
        {
            if (pawn?.health?.hediffSet == null) return;
            if (predicate == null) return;

            var hediffs = pawn.health.hediffSet.hediffs;
            if (hediffs == null || hediffs.Count == 0) return;

            // 阶段1: 收集符合条件的Hediff
            var toRemove = new List<Hediff>();
            foreach (var hediff in hediffs)
            {
                if (predicate(hediff))
                    toRemove.Add(hediff);
            }

            // 阶段2: 批量移除
            foreach (var hediff in toRemove)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        /// <summary>
        /// 检查身体部位的任意祖先是否不可用(MissingBodyPart或addedPartProps)。
        /// 用于快照恢复时跳过子部位的Hediff。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="part">要检查的身体部位</param>
        /// <returns>如果任意祖先不可用则返回true</returns>
        public static bool AnyAncestorIsUnavailable(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet == null) return false;
            if (part == null) return false;

            // 遍历祖先链
            var current = part.parent;
            while (current != null)
            {
                // 检查是否有MissingBodyPart
                if (pawn.health.hediffSet.PartIsMissing(current))
                    return true;

                // 检查是否是添加的部位(假肢等)
                if (pawn.health.hediffSet.HasDirectlyAddedPartFor(current))
                    return true;

                current = current.parent;
            }

            return false;
        }
    }
}
