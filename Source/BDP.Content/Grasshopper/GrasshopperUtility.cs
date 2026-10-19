using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using BDP.Content.GroundDash;

namespace BDP.Content.Grasshopper
{
    /// <summary>
    /// 蚱蜢工具类 - 处理跳跃核心逻辑。
    /// 新版不再依赖 CompApparelReloadable，Trion 成本由表达系统上游处理。
    /// </summary>
    public static class GrasshopperUtility
    {
        /// <summary>
        /// 执行蚱蜢跳跃 — 通过中性地面冲刺设施生成自定义 PawnFlyer。
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
            return GroundDashUtility.DoDash(pawn, targetCell, verbProps, pawnFlyerDef);
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
            return GroundDashUtility.CanReachRequestedCell(
                pawn.Map,
                root,
                cell,
                range)
                && GenSight.LineOfSight(root, cell, pawn.Map);
            }
            return false;
        }

        /// <summary>
        /// 判断目标单元格是否是有效的跳跃落点。
        /// 条件：在边界内、可行走、无阻挡、无关闭的门。
        /// </summary>
        public static bool ValidJumpTarget(Map map, IntVec3 cell)
        {
            return GroundDashUtility.IsValidDashCell(map, cell);
        }
    }
}
