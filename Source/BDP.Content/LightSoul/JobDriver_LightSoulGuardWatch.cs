using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BDP.Content.LightSoul
{
    /// <summary>
    /// 光魂举盾“注视警戒”作业。
    /// 它保留锁定目标，只在目标当前位于射程和视线内时临时接管朝向，不启动路径、施放或伤害流程。
    /// </summary>
    public sealed class JobDriver_LightSoulGuardWatch : JobDriver
    {
        /// <summary>
        /// 注视警戒不预约目标，因为它不会占用、接触或攻击目标。
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        /// <summary>
        /// 构造一个永不移动、每 tick（游戏刻）检查条件并按需注视目标的作业步骤。
        /// 暂时失去射程或视线只会暂停注视，不会结束 Job 或丢失目标。
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil watchTarget = ToilMaker.MakeToil("LightSoulGuardWatch");
            watchTarget.initAction = delegate
            {
                pawn.pather?.StopDead();
            };
            watchTarget.tickAction = delegate
            {
                Verb_LightSoulGuardWatch watchVerb = job.verbToUse as Verb_LightSoulGuardWatch;
                if (watchVerb == null
                    || LightSoulGuardWatchUtility.ResolveVerb(pawn) != watchVerb
                    || !IsTargetStillUsable(TargetA))
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                // 对齐原版强制攻击：不可命中时保留 Job 和目标，但把朝向交还原版；恢复后再接管。
                bool canWatchTarget = watchVerb.CanHitTarget(TargetA);
                watchTarget.handlingFacing = canWatchTarget;
                if (canWatchTarget)
                {
                    pawn.rotationTracker.FaceTarget(TargetA);
                }
            };
            watchTarget.defaultCompleteMode = ToilCompleteMode.Never;
            yield return watchTarget;
        }

        /// <summary>
        /// 判断锁定目标是否仍真实存在；射程和视线不属于目标失效条件。
        /// </summary>
        private bool IsTargetStillUsable(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return false;
            }

            if (!target.HasThing)
            {
                return target.Cell.IsValid && target.Cell.InBounds(Map);
            }

            Thing targetThing = target.Thing;
            return !targetThing.Destroyed
                && targetThing.Spawned
                && targetThing.Map == Map;
        }
    }
}
