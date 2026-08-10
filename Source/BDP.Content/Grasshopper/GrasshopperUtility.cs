using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BDP.Content.Grasshopper
{
    /// <summary>
    /// 蚱蜢工具类 - 处理跳跃核心逻辑。
    /// 新版不再依赖 CompApparelReloadable，Trion 成本由表达系统上游处理。
    /// </summary>
    public static class GrasshopperUtility
    {
        /// <summary>
        /// 执行蚱蜢跳跃 — 生成自定义 PawnFlyer 并发射 Pawn。
        /// </summary>
        /// <param name="pawn">跳跃的 Pawn</param>
        /// <param name="targetCell">目标单元格</param>
        /// <param name="verbProps">Verb 属性（读取飞行特效、落地音效等）</param>
        /// <param name="pawnFlyerDef">使用的 PawnFlyer ThingDef</param>
        /// <returns>是否成功生成 PawnFlyer</returns>
        public static bool DoJump(
            Pawn pawn,
            IntVec3 targetCell,
            VerbProperties verbProps,
            ThingDef pawnFlyerDef)
        {
            Map map = pawn.Map;
            IntVec3 position = pawn.Position;
            Vector3 vector = (targetCell - position).ToVector3();
            vector.Normalize();
            bool flag = Find.Selector.IsSelected(pawn);

            // 使用原版 PawnFlyer.MakeFlyer，传入自定义 PawnFlyer ThingDef
            PawnFlyer pawnFlyer = PawnFlyer.MakeFlyer(
                pawnFlyerDef,
                pawn,
                targetCell,
                verbProps?.flightEffecterDef,
                verbProps?.soundLanding,
                verbProps?.flyWithCarriedThing ?? false,
                null
            );

            if (pawnFlyer != null)
            {
                FleckMaker.ThrowDustPuff(position.ToVector3Shifted() - vector, map, 2f);
                GenSpawn.Spawn(pawnFlyer, targetCell, map);
                if (flag)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 为 Pawn 下达跳跃指令 — 寻找最佳可达目标位置并生成 Job。
        /// </summary>
        public static void OrderJump(Pawn pawn, LocalTargetInfo target, Verb verb, float range)
        {
            Map map = pawn.Map;
            IntVec3 intVec = RCellFinder.BestOrderedGotoDestNear(
                target.Cell, pawn,
                (IntVec3 c) => ValidJumpTarget(map, c)
                    && CanHitTargetFrom(pawn, pawn.Position, c, range));
            Job job = JobMaker.MakeJob(JobDefOf.CastJump, intVec);
            job.verbToUse = verb;
            if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                FleckMaker.Static(intVec, map, RimWorld.FleckDefOf.FeedbackGoto);
            }
        }

        /// <summary>
        /// 判断从 root 位置能否命中目标单元格（距离+视线检查）。
        /// </summary>
        public static bool CanHitTargetFrom(Pawn pawn, IntVec3 root, LocalTargetInfo targ, float range)
        {
            float num = range * range;
            IntVec3 cell = targ.Cell;
            if ((float)pawn.Position.DistanceToSquared(cell) <= num)
            {
                return GenSight.LineOfSight(root, cell, pawn.Map);
            }
            return false;
        }

        /// <summary>
        /// 判断目标单元格是否是有效的跳跃落点。
        /// 条件：在边界内、可行走、无阻挡、无关闭的门。
        /// </summary>
        public static bool ValidJumpTarget(Map map, IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }
            if (cell.Impassable(map) || !cell.Walkable(map) || cell.Fogged(map))
            {
                return false;
            }
            Building edifice = cell.GetEdifice(map);
            // C# 7.3兼容写法：is 模式匹配不适用，用 as + null 判断
            if (edifice != null)
            {
                Building_Door door = edifice as Building_Door;
                if (door != null && !door.Open)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
