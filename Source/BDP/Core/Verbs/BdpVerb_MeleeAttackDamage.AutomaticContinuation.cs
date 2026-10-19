using BDP.Core.AttackExecution;
using Verse;
using Verse.AI;

namespace BDP.Core.Verbs
{
    public partial class BdpVerb_MeleeAttackDamage
    {
        /// <summary>发起这一次有限自动计划的原版任务编号；不创建或延长该任务。</summary>
        private int automaticPlanJobId = -1;

        /// <summary>源任务当时的目标；包括没有任务目标的战斗等待，防止同任务原地换目标。</summary>
        private LocalTargetInfo automaticPlanJobTarget = LocalTargetInfo.Invalid;

        /// <summary>原版已经选定的本次攻击目标；续段不得另选目标。</summary>
        private Thing automaticPlanTarget;

        /// <summary>只有首段真正起手成功，才允许宿主自行推进这一次计划的剩余段。</summary>
        private bool automaticPlanStarted;

        /// <summary>随现有近战宿主存档，读档后的任务编号和目标必须仍然匹配。</summary>
        private void ExposeAutomaticContinuation()
        {
            Scribe_Values.Look(ref automaticPlanJobId, "automaticPlanJobId", -1);
            Scribe_TargetInfo.Look(ref automaticPlanJobTarget, "automaticPlanJobTarget");
            Scribe_References.Look(ref automaticPlanTarget, "automaticPlanTarget");
            Scribe_Values.Look(ref automaticPlanStarted, "automaticPlanStarted");
            // 此瞬时判定只有已起手的自动有限计划需要恢复，否则读档会把已完成首段误判为硬失败。
            bool lastShotCompleted = automaticPlanStarted && LastMeleeBurstShotCompleted;
            Scribe_Values.Look(ref lastShotCompleted, "automaticPlanLastShotCompleted");
            if (Scribe.mode == LoadSaveMode.LoadingVars && automaticPlanStarted)
            {
                LastMeleeBurstShotCompleted = lastShotCompleted;
            }
        }

        /// <summary>只为自动、立即、仍有后续的有限计划绑定原任务边界；手动任务不进入此路径。</summary>
        internal void BindAutomaticContinuation(Job sourceJob, Thing target)
        {
            CancelAutomaticContinuation(false);
            if (sourceJob == null || target == null || NextRuntimeStepIndex < 0
                || PlanReason != AttackExecutionReason.AutoMelee
                || PlanDispatchIntent != AttackDispatchIntent.ImmediateCast)
            {
                return;
            }

            // 预热可能在实际出手前取消，不能沿用该宿主上一轮的“已完成”判定。
            ClearLastMeleeBurstShotResult();
            automaticPlanJobId = sourceJob.loadID;
            automaticPlanJobTarget = sourceJob.targetA;
            automaticPlanTarget = target;
        }

        /// <summary>由真实起手成功分支激活，不能用原版 TryMeleeAttack 的返回值代替起手结果。</summary>
        internal void NotifyAutomaticPlanStarted()
        {
            automaticPlanStarted = automaticPlanJobId >= 0 && NextRuntimeStepIndex >= 0;
        }

        /// <summary>取消本控制器持有的剩余段；不清空正在执行的连击，不触碰原版任务。</summary>
        private void CancelAutomaticContinuation(bool cancelRemainingSteps = true)
        {
            if (cancelRemainingSteps && automaticPlanJobId >= 0)
            {
                NextRuntimeStepIndex = -1;
            }

            automaticPlanJobId = -1;
            automaticPlanJobTarget = LocalTargetInfo.Invalid;
            automaticPlanTarget = null;
            automaticPlanStarted = false;
        }

        /// <summary>原版任务、攻击者和目标仍有效，才允许消费已起手计划的下一段。</summary>
        private bool IsAutomaticContinuationCurrent(Thing requestedTarget)
        {
            Pawn pawn = CasterPawn;
            Job job = pawn?.CurJob;
            Pawn targetPawn = automaticPlanTarget as Pawn;
            return automaticPlanStarted && NextRuntimeStepIndex >= 0
                && PlanReason == AttackExecutionReason.AutoMelee
                && PlanDispatchIntent == AttackDispatchIntent.ImmediateCast
                && pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed
                && job != null && job.loadID == automaticPlanJobId && job.targetA == automaticPlanJobTarget
                && requestedTarget == automaticPlanTarget && automaticPlanTarget != null
                && automaticPlanTarget.Spawned && !automaticPlanTarget.Destroyed
                && (targetPawn == null || (!targetPawn.Dead && (!targetPawn.Downed || job.killIncappedTarget)))
                && AttackExecutionPostLoadRecovery.IsCurrentAttackSessionValid(this);
        }

        /// <summary>
        /// 原版再次请求同目标攻击时，优先交付同一次计划的剩余段，避免总是重排首手。
        /// 仅双持主手到副手的完整冷却允许准备下一侧；本方法不起手、不派工、不追击。
        /// </summary>
        internal bool TryPrepareAutomaticContinuation(Thing requestedTarget, out Verb verb)
        {
            verb = null;
            if (!automaticPlanStarted)
            {
                return false;
            }

            if (!IsAutomaticContinuationCurrent(requestedTarget))
            {
                CancelAutomaticContinuation();
                return false;
            }

            if (WarmingUp
                || Bursting
                || CasterPawn.stances == null
                || (CasterPawn.stances.FullBodyBusy && !CanSkipDualMeleeHandoffCooldown()))
            {
                verb = this;
                return true;
            }

            // miss/dodge（未命中/闪避）可续段；目标或攻击者硬失败不可续段。
            if (!LastMeleeBurstShotCompleted)
            {
                CancelAutomaticContinuation();
                return false;
            }

            Job sourceJob = CasterPawn.CurJob;
            LocalTargetInfo sourceJobTarget = automaticPlanJobTarget;
            bool prepared = new MeleeVerbContinuationPlanner().TryPrepareNextRun(this, requestedTarget, out MeleeAttackExecutionContext context);
            BdpVerb_MeleeAttackDamage nextVerb = prepared ? context?.Verb as BdpVerb_MeleeAttackDamage : null;
            if (!prepared || nextVerb == null || context.Target.Thing != requestedTarget
                || !nextVerb.CanHitTarget(context.Target)
                || CasterPawn.CurJob != sourceJob || sourceJob.targetA != sourceJobTarget)
            {
                CancelAutomaticContinuation();
                if (nextVerb != null)
                {
                    nextVerb.CancelAutomaticContinuation();
                    nextVerb.NextRuntimeStepIndex = -1;
                }
                return false;
            }

            // 规划器已经绑定新上下文；同宿主续段时不能再抹掉刚绑定的下一段索引。
            if (nextVerb != this)
            {
                CancelAutomaticContinuation();
            }
            nextVerb.BindAutomaticContinuation(sourceJob, requestedTarget);
            verb = nextVerb;
            return true;
        }

        /// <summary>复用正式宿主更新队列推进有限计划，只跨过双持主手到副手之间的完整冷却。</summary>
        internal void TickAutomaticContinuation()
        {
            if (!automaticPlanStarted)
            {
                return;
            }

            Thing target = automaticPlanTarget;
            if (!IsAutomaticContinuationCurrent(target))
            {
                CancelAutomaticContinuation();
                return;
            }

            if (WarmingUp
                || Bursting
                || CasterPawn.stances == null
                || (CasterPawn.stances.FullBodyBusy && !CanSkipDualMeleeHandoffCooldown()))
            {
                return;
            }

            bool isSurpriseAttack = surpriseAttack;
            if (TryPrepareAutomaticContinuation(target, out Verb nextVerb)
                && !nextVerb.TryStartCastOn(target, surpriseAttack: isSurpriseAttack))
            {
                ((BdpVerb_MeleeAttackDamage)nextVerb).CancelAutomaticContinuation();
            }
        }
    }
}
