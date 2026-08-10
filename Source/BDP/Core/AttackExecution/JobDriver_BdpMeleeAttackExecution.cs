using System.Collections.Generic;
using BDP.Core.Verbs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// BDP 近战正式执行推进器。
    /// 它只负责把已经确认好的近战步骤追上并打完，不回表达层重新选攻击。
    /// </summary>
    internal sealed class JobDriver_BdpMeleeAttackExecution : JobDriver
    {
        /// <summary>
        /// 当前近战执行器使用的续段规划器。
        /// 它负责在一段 run 打完后，按原始正式计划重建下一段。
        /// </summary>
        private readonly MeleeVerbContinuationPlanner continuationPlanner = new MeleeVerbContinuationPlanner();

        /// <summary>
        /// 当前已经成功启动过多少次近战施放。
        /// </summary>
        private int castCount;

        private int currentStepIndex;

        private int nextStepDelayTicks;

        /// <summary>
        /// 序列化近战推进器自身的轻量状态。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref castCount, "castCount", 0);
            Scribe_Values.Look(ref currentStepIndex, "currentStepIndex", 0);
            Scribe_Values.Look(ref nextStepDelayTicks, "nextStepDelayTicks", 0);
        }

        /// <summary>
        /// 近战攻击需要按原版方式预约攻击目标。
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (job != null && job.targetA.Thing is IAttackTarget target)
            {
                pawn.Map.attackTargetReservationManager.Reserve(pawn, job, target);
            }

            return true;
        }

        /// <summary>
        /// 构造最小近战推进 toils。
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            Toil followAndAttack = ToilMaker.MakeToil("BdpMeleeAttackExecution");
            followAndAttack.tickIntervalAction = delegate
            {
                if (!TryTickExecution())
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            followAndAttack.activeSkill = () => SkillDefOf.Melee;
            followAndAttack.defaultCompleteMode = ToilCompleteMode.Never;
            followAndAttack.FailOnDespawnedOrNull(TargetIndex.A);
            yield return followAndAttack;
        }

        /// <summary>
        /// 推进当前近战执行 job。
        /// </summary>
        private bool TryTickExecution()
        {
            Thing targetThing = job != null ? job.GetTarget(TargetIndex.A).Thing : null;
            if (targetThing == null || !targetThing.Spawned)
            {
                EndJobWith(JobCondition.Succeeded);
                return true;
            }

            Pawn targetPawn = targetThing as Pawn;
            if (targetPawn != null && targetPawn.IsPsychologicallyInvisible())
            {
                EndJobWith(JobCondition.Succeeded);
                return true;
            }

            Verb verb = job != null ? job.verbToUse : null;
            if (verb == null)
            {
                return false;
            }

            if (!AttackExecutionPostLoadRecovery.IsCurrentAttackSessionValid(verb))
            {
                pawn.stances?.CancelBusyStanceHard();
                EndJobWith(JobCondition.InterruptForced);
                return true;
            }

            if (nextStepDelayTicks > 0)
            {
                nextStepDelayTicks--;
                return true;
            }

            if (castCount >= ResolveRequiredCastCount())
            {
                return TryContinueWithPreparedRun(targetThing, verb);
            }

            if (verb.state == VerbState.Bursting)
            {
                return true;
            }

            if (ShouldWaitForBusyStance(verb))
            {
                return true;
            }

            if (!pawn.CanReachImmediate(targetThing, PathEndMode.Touch))
            {
                if (targetThing != pawn.pather.Destination.Thing || !pawn.pather.Moving)
                {
                    pawn.pather.StartPath(targetThing, PathEndMode.Touch);
                }

                return true;
            }

            if (targetPawn != null && targetPawn.Downed && (job == null || !job.killIncappedTarget))
            {
                EndJobWith(JobCondition.Succeeded);
                return true;
            }

            TryBindCurrentStepToolSurface(verb, targetThing);
            if (!pawn.meleeVerbs.TryMeleeAttack(targetThing, verb))
            {
                return false;
            }

            nextStepDelayTicks = ResolveIntervalTicksAfterStep(verb, currentStepIndex);
            castCount++;
            currentStepIndex = ResolveNextStepIndex(verb, currentStepIndex);
            if (IsCurrentRunConsumed(verb))
            {
                return TryContinueWithPreparedRun(targetThing, verb);
            }

            return true;
        }

        /// <summary>
        /// 读取本次 job 需要成功启动的近战施放次数。
        /// </summary>
        private int ResolveRequiredCastCount()
        {
            return job != null && job.maxNumMeleeAttacks > 0
                ? job.maxNumMeleeAttacks
                : 1;
        }

        /// <summary>
        /// 在每刀真正起手前，把当前 formal host 切到这一刀对应的 Tool 表面。
        /// 新一轮开始时顺便预排下一轮的 step-tool 序列。
        /// </summary>
        private void TryBindCurrentStepToolSurface(Verb verb, Thing targetThing)
        {
            if (!(verb is BdpVerb_FormalHostMelee formalHost))
            {
                return;
            }

            int stepCount = formalHost.ResolvePreparedStepCount();
            if (stepCount <= 0)
            {
                stepCount = 1;
            }

            if (currentStepIndex == 0
                || formalHost.ResolvePreparedStepToolCount() != stepCount)
            {
                int roundOrdinal = castCount / stepCount;
                formalHost.PrepareStepToolSequenceForCurrentRound(targetThing, stepCount, roundOrdinal);
            }

            formalHost.ApplyStepToolSurface(currentStepIndex);
        }

        private static int ResolveIntervalTicksAfterStep(Verb verb, int currentStepIndex)
        {
            if (verb is BdpVerb_MeleeAttackDamage meleeVerb)
            {
                return meleeVerb.ResolveIntervalTicksAfterStep(currentStepIndex);
            }

            return 0;
        }

        private static int ResolveNextStepIndex(Verb verb, int currentStepIndex)
        {
            if (verb is BdpVerb_MeleeAttackDamage meleeVerb)
            {
                int stepCount = meleeVerb.ResolvePreparedStepCount();
                if (stepCount > 0)
                {
                    return (currentStepIndex + 1) % stepCount;
                }
            }

            return currentStepIndex + 1;
        }

        /// <summary>
        /// 判断当前近战 run 是否已经把本轮声明的 step 全部消费完。
        /// run 消费完后，JobDriver 会优先尝试续到下一段，而不是直接结束。
        /// </summary>
        private bool IsCurrentRunConsumed(Verb verb)
        {
            if (!(verb is BdpVerb_MeleeAttackDamage meleeVerb))
            {
                return castCount > 0;
            }

            int stepCount = meleeVerb.ResolvePreparedStepCount();
            if (stepCount <= 0)
            {
                return castCount > 0;
            }

            return castCount > 0
                && currentStepIndex == 0
                && castCount % stepCount == 0;
        }

        /// <summary>
        /// 当前 run 打完后，优先尝试续接下一段近战计划。
        /// 只有没有后续 run 时，才把本次 job 正常结束。
        /// </summary>
        private bool TryContinueWithPreparedRun(Thing targetThing, Verb verb)
        {
            if (!(verb is BdpVerb_MeleeAttackDamage meleeVerb))
            {
                EndJobWith(JobCondition.Succeeded);
                return true;
            }

            if (!meleeVerb.HasPendingContinuation())
            {
                string noContinuationReason = IsPersistentAttackOrder(meleeVerb.PlanDispatchIntent)
                    ? "persistent_run_missing"
                    : "plan_consumed";
                AttackExecutionDiagnostics.LogMeleeContinuationEnd(
                    pawn,
                    verb,
                    meleeVerb.PlanSessionToken,
                    meleeVerb.NextRuntimeStepIndex,
                    noContinuationReason);
                if (noContinuationReason == "persistent_run_missing")
                {
                    return false;
                }

                EndJobWith(JobCondition.Succeeded);
                return true;
            }

            LocalTargetInfo continuationTarget = targetThing;
            AttackExecutionDiagnostics.LogMeleeContinuationPrepare(
                pawn,
                verb,
                meleeVerb.PlanSessionToken,
                continuationTarget,
                meleeVerb.NextRuntimeStepIndex);
            if (!continuationPlanner.TryPrepareNextRun(meleeVerb, continuationTarget, out MeleeAttackExecutionContext nextContext))
            {
                AttackExecutionDiagnostics.LogMeleeContinuationEnd(
                    pawn,
                    verb,
                    meleeVerb.PlanSessionToken,
                    meleeVerb.NextRuntimeStepIndex,
                    "prepare_failed");
                return false;
            }

            if (job != null)
            {
                job.verbToUse = nextContext.Verb;
                job.targetA = nextContext.Target;
                job.maxNumMeleeAttacks = nextContext.RequiredStepCount;
            }

            castCount = 0;
            currentStepIndex = 0;
            nextStepDelayTicks = 0;
            AttackExecutionDiagnostics.LogMeleeContinuationSwitch(
                pawn,
                verb,
                nextContext.Verb,
                nextContext.PlanSessionToken,
                nextContext.Target,
                nextContext.NextRuntimeStepIndex);
            return true;
        }

        private static bool IsPersistentAttackOrder(AttackDispatchIntent dispatchIntent)
        {
            return dispatchIntent == AttackDispatchIntent.ForceTargetOrder
                || dispatchIntent == AttackDispatchIntent.AutoAttackOrder;
        }

        /// <summary>
        /// BDP 近战 step 调度拥有连击内部的节奏控制权。
        /// 若当前 busy stance 只是上一段同一条 formal melee verb 留下的 cooldown，
        /// 且 step 间延迟已经由 JobDriver 自己走完，就主动释放该 stance，
        /// 让下一段继续按准备好的 step 推进。
        /// </summary>
        private bool ShouldWaitForBusyStance(Verb verb)
        {
            if (pawn?.stances == null || !pawn.stances.FullBodyBusy)
            {
                return false;
            }

            if (TryConsumeOwnedCooldownStance(verb))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 只消费当前连击内部、由同一条 formal melee verb 留下的 cooldown stance。
        /// 外部打断、其它 verb 的忙碌态、以及 warmup/burst 过程都不在这里清除。
        /// </summary>
        private bool TryConsumeOwnedCooldownStance(Verb verb)
        {
            if (castCount <= 0)
            {
                return false;
            }

            if (currentStepIndex == 0)
            {
                return false;
            }

            Stance_Busy busyStance = pawn?.stances?.curStance as Stance_Busy;
            if (!(busyStance is Stance_Cooldown))
            {
                return false;
            }

            if (busyStance.verb == verb)
            {
                pawn.stances.CancelBusyStanceHard();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断后续 job 是否仍属于同一目标连续推进。
        /// </summary>
        public override bool IsContinuation(Job job)
        {
            return this.job != null
                && job != null
                && this.job.GetTarget(TargetIndex.A) == job.GetTarget(TargetIndex.A);
        }
    }
}
