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
        /// 当前连续攻击轮次已经成功启动过多少次施放。
        /// 多击由同一个原版 burst 承担，因此一次轮次只会增加一次。
        /// </summary>
        private int castCount;

        /// <summary>
        /// 序列化近战推进器自身的轻量状态。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref castCount, "castCount", 0);
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

            // TryStartCastOn 成功后，预热和 burst 的每一击都由原版 VerbTick 推进。
            // JobDriver 不能在中间重新起手，否则会重复预热并破坏整轮冷却。
            if (verb.WarmingUp || verb.state == VerbState.Bursting)
            {
                return true;
            }

            if (ShouldWaitForBusyStance(verb))
            {
                return true;
            }

            if (castCount > 0)
            {
                // 普通最终冷却结束后才续接；仅双持主手到副手的交接允许提前续接。
                if (!TryContinueWithPreparedRun(targetThing, verb))
                {
                    return false;
                }

                // 续段也可能正常结束或切换作业；旧推进器不能再起手。
                if (pawn.CurJob != job || pawn.jobs.curDriver != this)
                {
                    return true;
                }

                // 准备成功后必须同次更新起手；额外空档会让原版把征召人物转回朝南。
                // 续段会重绑动作和目标，不能沿用上轮的局部变量。
                targetThing = job.GetTarget(TargetIndex.A).Thing;
                targetPawn = targetThing as Pawn;
                if (targetThing == null
                    || !targetThing.Spawned
                    || (targetPawn != null && targetPawn.IsPsychologicallyInvisible()))
                {
                    EndJobWith(JobCondition.Succeeded);
                    return true;
                }

                verb = job.verbToUse;
                if (verb == null)
                {
                    return false;
                }
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
            bool canHitCurrentTarget = verb.CanHitTarget(targetThing);
            bool canHitTargetFromCurrentPos = verb.CanHitTargetFrom(pawn.Position, targetThing);
            int requiredCastCount = ResolveJobCastLimit();
            AttackExecutionDiagnostics.LogContinuousJobCastAttempt(
                pawn,
                verb,
                targetThing,
                castCount,
                requiredCastCount,
                canHitCurrentTarget,
                canHitTargetFromCurrentPos);

            bool started = verb.TryStartCastOn(targetThing);
            AttackExecutionDiagnostics.LogContinuousJobCastResult(
                pawn,
                verb,
                targetThing,
                castCount,
                requiredCastCount,
                started ? "started" : "verb_start_returned_false",
                started,
                canHitTargetFromCurrentPos,
                false,
                verb.state.ToString());
            if (!started)
            {
                return false;
            }

            // 只有原版明确接受了这次起手，才推进施放计数。
            castCount = 1;
            return true;
        }

        /// <summary>
        /// 读取 job 的连续轮次上限，仅用于诊断和状态说明。
        /// 连续多击的每击数量不由 job 计数，而由原版 Verb burstShotCount 承担。
        /// </summary>
        private int ResolveJobCastLimit()
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

            const int burstStepCount = 1;
            if (formalHost.ResolvePreparedStepToolCount() != burstStepCount)
            {
                formalHost.PrepareStepToolSequenceForCurrentRound(targetThing, burstStepCount, castCount);
            }

            formalHost.ApplyStepToolSurface(stepIndex: 0);
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
            AttackExecutionDiagnostics.LogMeleeContinuationSwitch(
                pawn,
                verb,
                nextContext.Verb,
                nextContext.PlanSessionToken,
                nextContext.Target,
                nextContext.NextRuntimeStepIndex);
            return true;
        }

        /// <summary>
        /// 判断本次派单是否要求持续攻击，整轮结束后仍需继续准备下一轮。
        /// </summary>
        private static bool IsPersistentAttackOrder(AttackDispatchIntent dispatchIntent)
        {
            return dispatchIntent == AttackDispatchIntent.ForceTargetOrder
                || dispatchIntent == AttackDispatchIntent.AutoAttackOrder;
        }

        /// <summary>
        /// 等待原版忙碌姿态；仅双持主手到副手的完整冷却允许按计划提前续接。
        /// 连击内部间隔、普通近战冷却和双持副手后的最终冷却仍由原版 Verb 生命周期推进。
        /// </summary>
        private bool ShouldWaitForBusyStance(Verb verb)
        {
            if (pawn?.stances == null || !pawn.stances.FullBodyBusy)
            {
                return false;
            }

            return !(verb is BdpVerb_MeleeAttackDamage meleeVerb
                && meleeVerb.CanSkipDualMeleeHandoffCooldown());
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
