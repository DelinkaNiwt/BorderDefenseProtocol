using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedProtocol;
using BDP.Core.Verbs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// BDP 远程正式执行推进器。
    /// 它只负责持续推进已经确认好的远程步骤，不再回到表达层重新选攻击。
    /// </summary>
    internal sealed class JobDriver_BdpRangedAttackExecution : JobDriver
    {
        /// <summary>
        /// 记录目标在 job 启动时是否已倒地，沿用原版静态攻击的结束规则。
        /// </summary>
        private bool startedIncapacitated;

        /// <summary>
        /// 当前已经成功启动过多少次施放。
        /// </summary>
        private int castCount;

        /// <summary>
        /// 当前 job 拥有的 cleanup 会话快照。
        /// 旧 job 退出时只能清掉自己这代会话，不能误清同壳上的新会话。
        /// </summary>
        private string cleanupOwnedAttackInstanceId;
        private string cleanupOwnedResultId;
        private int cleanupOwnedProjectionVersion;
        private string cleanupOwnedOwnerPawnThingId;

        /// <summary>
        /// 序列化远程推进器自身的轻量状态。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startedIncapacitated, "startedIncapacitated", false);
            Scribe_Values.Look(ref castCount, "castCount", 0);
            Scribe_Values.Look(ref cleanupOwnedAttackInstanceId, "cleanupOwnedAttackInstanceId");
            Scribe_Values.Look(ref cleanupOwnedResultId, "cleanupOwnedResultId");
            Scribe_Values.Look(ref cleanupOwnedProjectionVersion, "cleanupOwnedProjectionVersion", 0);
            Scribe_Values.Look(ref cleanupOwnedOwnerPawnThingId, "cleanupOwnedOwnerPawnThingId");
        }

        /// <summary>
        /// 当前阶段不需要额外抢占保留。
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        /// <summary>
        /// 构造最小远程推进 toils。
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(CleanupAttackSessionOnJobExit);
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            Toil attackToil = ToilMaker.MakeToil("BdpRangedAttackExecution");
            attackToil.initAction = delegate
            {
                LocalTargetInfo liveValidationTarget = ResolveLiveValidationTarget(job != null ? job.verbToUse : null);
                if (liveValidationTarget.HasThing && liveValidationTarget.Thing is Pawn targetPawn)
                {
                    startedIncapacitated = targetPawn.Downed;
                }

                CaptureCleanupOwnedSessionIfNeeded(job != null ? job.verbToUse : null);
                pawn.pather.StopDead();
            };
            attackToil.tickIntervalAction = delegate
            {
                if (!TryTickExecution())
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            attackToil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return attackToil;
        }

        /// <summary>
        /// 推进当前远程执行 job。
        /// </summary>
        private bool TryTickExecution()
        {
            if (!TargetA.IsValid)
            {
                EndJobWith(JobCondition.Succeeded);
                return true;
            }

            Verb verb = job != null ? job.verbToUse : null;
            if (!TryValidateLiveTarget(verb))
            {
                EndJobWith(JobCondition.Succeeded);
                return true;
            }

            if (verb == null)
            {
                return false;
            }

            CaptureCleanupOwnedSessionIfNeeded(verb);

            if (!AttackExecutionPostLoadRecovery.IsCurrentAttackSessionValid(verb))
            {
                AttackExecutionDiagnostics.LogRangedJobSessionInvalid(
                    pawn,
                    verb,
                    TargetA,
                    castCount,
                    ResolveRequiredCastCount(),
                    verb is BdpVerb_Shoot shootVerb ? shootVerb.HostSessionToken : null,
                    verb.state.ToString());
                pawn.stances?.CancelBusyStanceHard();
                EndJobWith(JobCondition.InterruptForced);
                return true;
            }

            if (pawn.stances.FullBodyBusy)
            {
                return true;
            }

            if (castCount >= ResolveRequiredCastCount())
            {
                EndJobWith(JobCondition.Succeeded);
                return true;
            }

            if (!CanHitCurrentTarget(verb))
            {
                AttackExecutionDiagnostics.LogContinuousJobCastResult(
                    pawn,
                    verb,
                    TargetA,
                    castCount,
                    ResolveRequiredCastCount(),
                    "target_out_of_range",
                    false,
                    verb.CanHitTargetFrom(pawn.Position, TargetA),
                    job != null && job.endIfCantShootTargetFromCurPos,
                    verb.state.ToString());
                // 原版 AttackStatic 在 endIfCantShootTargetFromCurPos=false 时，
                // 会保留目标 job，等待下一次 tick 重新尝试。
                return true;
            }

            LocalTargetInfo castTarget = ResolveCastTarget(verb);
            bool canHitTargetFromCurrentPos = verb.CanHitTargetFrom(pawn.Position, TargetA);
            if (!canHitTargetFromCurrentPos)
            {
                AttackExecutionDiagnostics.LogContinuousJobCastResult(
                    pawn,
                    verb,
                    castTarget,
                    castCount,
                    ResolveRequiredCastCount(),
                    "target_not_currently_hittable",
                    false,
                    false,
                    job != null && job.endIfCantShootTargetFromCurPos,
                    verb.state.ToString());
                // 视线或其它当前命中条件暂时不满足时保留 job，
                // 让目标重新进入可攻击状态后自然恢复暖机。
                return true;
            }

            AttackExecutionDiagnostics.LogContinuousJobCastAttempt(
                pawn,
                verb,
                castTarget,
                castCount,
                ResolveRequiredCastCount(),
                true,
                canHitTargetFromCurrentPos);
            if (!PrepareNextCast(verb, castTarget))
            {
                AttackExecutionDiagnostics.LogContinuousJobCastResult(
                    pawn,
                    verb,
                    castTarget,
                    castCount,
                    ResolveRequiredCastCount(),
                    "prepare_failed",
                    false,
                    canHitTargetFromCurrentPos,
                    job != null && job.endIfCantShootTargetFromCurPos,
                    verb.state.ToString());
                return false;
            }

            bool started = verb.TryStartCastOn(castTarget, false, true, job != null && job.preventFriendlyFire);
            AttackExecutionDiagnostics.LogContinuousJobCastResult(
                pawn,
                verb,
                castTarget,
                castCount,
                ResolveRequiredCastCount(),
                started ? "started" : "verb_start_returned_false",
                started,
                canHitTargetFromCurrentPos,
                job != null && job.endIfCantShootTargetFromCurPos,
                verb.state.ToString());
            if (started)
            {
                castCount++;
                return true;
            }

            if (job != null && job.endIfCantShootTargetFromCurPos && !verb.CanHitTargetFrom(pawn.Position, TargetA))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 按原版静态远程攻击规则检查当前目标是否还值得继续打。
        /// </summary>
        private bool TryValidateLiveTarget(Verb verb)
        {
            return IsLiveValidationTargetStillUsable(
                ResolveLiveValidationTarget(verb),
                startedIncapacitated);
        }

        /// <summary>
        /// 解析持续攻击真正应该用于存活校验的目标。
        /// 如果远程协议保存了实体语义目标，实体优先；没有实体语义目标时才回退到 job 的 TargetA。
        /// </summary>
        private LocalTargetInfo ResolveLiveValidationTarget(Verb verb)
        {
            return TryResolveSemanticValidationTarget(verb, out LocalTargetInfo semanticTarget)
                ? semanticTarget
                : TargetA;
        }

        /// <summary>
        /// 从 BDP 远程宿主读取当前攻击冻结的语义目标。
        /// 这里只接管实体目标，避免毒蛇这类路径攻击把首段地格误当成最终存活目标。
        /// </summary>
        private static bool TryResolveSemanticValidationTarget(Verb verb, out LocalTargetInfo target)
        {
            target = LocalTargetInfo.Invalid;
            if (!(verb is BdpVerb_Shoot shootVerb)
                || !shootVerb.TryResolveCurrentSemanticTarget(out target)
                || !target.HasThing)
            {
                target = LocalTargetInfo.Invalid;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 按原版静态远程攻击规则检查目标是否还值得继续打。
        /// 语义实体目标和普通 TargetA 共用同一套死亡、销毁、倒地、隐形规则。
        /// </summary>
        private static bool IsLiveValidationTargetStillUsable(LocalTargetInfo target, bool startedIncapacitated)
        {
            if (!target.HasThing)
            {
                return true;
            }

            Thing targetThing = target.Thing;
            Pawn targetPawn = targetThing as Pawn;
            if (targetThing.Destroyed || targetThing.Map == null)
            {
                return false;
            }

            if (targetPawn != null && targetPawn.Dead)
            {
                return false;
            }

            if (targetPawn != null && !startedIncapacitated && targetPawn.Downed)
            {
                return false;
            }

            return targetPawn == null || !targetPawn.IsPsychologicallyInvisible();
        }

        /// <summary>
        /// 检查当前目标是否仍在允许射程内。
        /// </summary>
        private bool CanHitCurrentTarget(Verb verb)
        {
            float maxRange = verb.verbProps != null ? verb.verbProps.range : 0f;
            if (maxRange <= 0f)
            {
                return false;
            }

            return (float)pawn.Position.DistanceToSquared(TargetA.Cell) <= maxRange * maxRange;
        }

        /// <summary>
        /// 读取本次 job 需要成功启动的施放次数。
        /// </summary>
        private int ResolveRequiredCastCount()
        {
            return job != null && job.maxNumStaticAttacks > 0
                ? job.maxNumStaticAttacks
                : 1;
        }

        /// <summary>
        /// 按 Verb 自己的规则决定最终施放目标。
        /// </summary>
        private LocalTargetInfo ResolveCastTarget(Verb verb)
        {
            if (verb?.verbProps != null && verb.verbProps.ai_RangedAlawaysShootGroundBelowTarget)
            {
                return TargetA.Cell;
            }

            return TargetA;
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

        /// <summary>
        /// 为下一轮持续推进准备完整远程会话。
        /// 真正的暖机、burst 推进和冷却仍然交给原版 Verb 会话驱动。
        /// </summary>
        private bool PrepareNextCast(Verb verb, LocalTargetInfo target)
        {
            if (!(verb is BdpVerb_Shoot shootVerb))
            {
                return true;
            }

            return shootVerb.PrepareContinuation(
                target,
                AttackExecutionReason.Manual,
                AttackDispatchIntent.ForceTargetOrder);
        }

        /// <summary>
        /// 在当前 job 首次真正拿到有效 formal host 会话后，锁存属于自己的 cleanup 代际快照。
        /// 一旦锁存完成，后续 tick 即使壳被新会话接管，也不允许被覆盖。
        /// </summary>
        private void CaptureCleanupOwnedSessionIfNeeded(Verb verb)
        {
            if (HasCapturedCleanupOwnedSession()
                || !(verb is BdpVerb_Shoot shootVerb)
                || shootVerb.HostSessionToken == null
                || !shootVerb.HostSessionToken.IsValid)
            {
                return;
            }

            AttackSessionToken token = shootVerb.HostSessionToken;
            cleanupOwnedAttackInstanceId = token.AttackInstanceId;
            cleanupOwnedResultId = token.ResultId;
            cleanupOwnedProjectionVersion = token.ProjectionVersion;
            cleanupOwnedOwnerPawnThingId = token.OwnerPawnThingId;
        }

        /// <summary>
        /// 判断当前 formal host 壳上的会话是否仍然属于这个 job 自己那一代。
        /// </summary>
        private bool ShouldCleanupCurrentVerbSession(BdpVerb_Shoot shootVerb)
        {
            if (!HasCapturedCleanupOwnedSession() || shootVerb?.HostSessionToken == null)
            {
                return false;
            }

            AttackSessionToken token = shootVerb.HostSessionToken;
            return token.AttackInstanceId == cleanupOwnedAttackInstanceId
                && token.ResultId == cleanupOwnedResultId
                && token.ProjectionVersion == cleanupOwnedProjectionVersion
                && token.OwnerPawnThingId == cleanupOwnedOwnerPawnThingId;
        }

        private bool HasCapturedCleanupOwnedSession()
        {
            return !string.IsNullOrWhiteSpace(cleanupOwnedResultId)
                && cleanupOwnedProjectionVersion > 0
                && !string.IsNullOrWhiteSpace(cleanupOwnedOwnerPawnThingId);
        }

        private AttackSessionToken ResolveCleanupOwnedSessionToken()
        {
            if (!HasCapturedCleanupOwnedSession())
            {
                return null;
            }

            return new AttackSessionToken
            {
                AttackInstanceId = cleanupOwnedAttackInstanceId,
                ResultId = cleanupOwnedResultId,
                ProjectionVersion = cleanupOwnedProjectionVersion,
                OwnerPawnThingId = cleanupOwnedOwnerPawnThingId
            };
        }

        /// <summary>
        /// 手动攻击 execution job 结束后，清掉绑定在 formal host verb 上的会话残留。
        /// 否则被打断的手动 resident session 会压过后续自动攻击新种下的 staged session。
        /// </summary>
        private void CleanupAttackSessionOnJobExit(JobCondition condition)
        {
            Verb verb = job != null ? job.verbToUse : null;
            AttackSessionToken ownedToken = ResolveCleanupOwnedSessionToken();
            if (!(verb is BdpVerb_Shoot shootVerb))
            {
                AttackExecutionDiagnostics.LogRangedJobCleanupDecision(
                    pawn,
                    verb,
                    ownedToken,
                    null,
                    condition,
                    false,
                    "skip_not_bdp_shoot");
                return;
            }

            AttackSessionToken currentToken = shootVerb.HostSessionToken != null
                ? shootVerb.HostSessionToken.Clone()
                : null;
            bool willReset = ShouldCleanupCurrentVerbSession(shootVerb);
            string reason;
            if (willReset)
            {
                reason = "apply_owned_session_reset";
            }
            else if (!HasCapturedCleanupOwnedSession())
            {
                reason = "skip_owned_snapshot_missing";
            }
            else if (currentToken == null)
            {
                reason = "skip_token_null";
            }
            else
            {
                reason = "skip_generation_mismatch";
            }

            AttackExecutionDiagnostics.LogRangedJobCleanupDecision(
                pawn,
                shootVerb,
                ownedToken,
                currentToken,
                condition,
                willReset,
                reason);

            if (ShouldCleanupCurrentVerbSession(shootVerb))
            {
                shootVerb.Reset();
            }
        }
    }
}
