using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BDP.Trigger
{
    /// <summary>
    /// 蚱蜢工具类 - 处理跳跃逻辑
    /// </summary>
    public static class GrasshopperUtility
    {
        public static bool DoJump(Pawn pawn, LocalTargetInfo currentTarget, CompApparelReloadable comp, VerbProperties verbProps, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo), ThingDef pawnFlyerOverride = null)
        {
            if (comp != null && !comp.CanBeUsed(out var _))
            {
                return false;
            }
            comp?.UsedOnce();

            IntVec3 position = pawn.Position;
            IntVec3 cell = currentTarget.Cell;
            Vector3 vector = (cell - position).ToVector3();
            vector.Normalize();
            Map map = pawn.Map;
            bool flag = Find.Selector.IsSelected(pawn);

            // 使用原版的PawnFlyer.MakeFlyer，传入我们自定义的PawnFlyer ThingDef
            PawnFlyer pawnFlyer = PawnFlyer.MakeFlyer(
                pawnFlyerOverride ?? Core.BDP_DefOf.BDP_PawnFlyer_Grasshopper,
                pawn,
                cell,
                verbProps.flightEffecterDef,
                verbProps.soundLanding,
                verbProps.flyWithCarriedThing,
                null
            );

            if (pawnFlyer != null)
            {
                FleckMaker.ThrowDustPuff(position.ToVector3Shifted() - vector, map, 2f);
                GenSpawn.Spawn(pawnFlyer, cell, map);
                if (flag)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
                return true;
            }
            return false;
        }

        public static void OrderJump(Pawn pawn, LocalTargetInfo target, Verb verb, float range)
        {
            Map map = pawn.Map;
            IntVec3 intVec = RCellFinder.BestOrderedGotoDestNear(target.Cell, pawn, (IntVec3 c) => ValidJumpTarget(map, c) && CanHitTargetFrom(pawn, pawn.Position, c, range));
            Job job = JobMaker.MakeJob(JobDefOf.CastJump, intVec);
            job.verbToUse = verb;
            if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                FleckMaker.Static(intVec, map, RimWorld.FleckDefOf.FeedbackGoto);
            }
        }

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
            // C# 7.3兼容写法
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
