using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Verbs;
using Verse;
using Verse.AI;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// BDP 攻击会话的版本收口与读档恢复器。
    /// 它统一负责“当前会话是否还能继续”和“读档后是否应该续接到当前已发布版本”。
    /// </summary>
    internal static class AttackExecutionPostLoadRecovery
    {
        /// <summary>
        /// 在读档边界收口或续接当前 Pawn 挂着的 BDP 攻击会话。
        /// 如果业务身份仍成立，就把 loaded formal host 会话重绑到当前已发布投影版本；否则结束旧会话。
        /// </summary>
        public static void RecoverStaleAttackSession(Pawn pawn)
        {
            ReconcileAttackSession(pawn, allowLoadedProjectionRebind: true);
        }

        /// <summary>
        /// 在已发布投影换版后，主动中断当前已经失效的 BDP 攻击会话。
        /// 这条路径不允许把旧会话偷偷续接到新版本，只负责“打断，然后交还上层”。
        /// </summary>
        internal static void InterruptInvalidAttackSession(Pawn pawn)
        {
            ReconcileAttackSession(pawn, allowLoadedProjectionRebind: false);
        }

        /// <summary>
        /// 判断一条 formal host 会话在当前已发布投影下是否仍然有效。
        /// 这条帮助口服务 live invalidation 守卫，不承担会话重绑。
        /// </summary>
        internal static bool IsCurrentAttackSessionValid(Verb verb)
        {
            return ValidateCurrentAttackSession(verb, allowLoadedProjectionRebind: false)
                == AttackSessionValidationState.Valid;
        }

        /// <summary>
        /// 按当前上下文统一收口或续接 BDP 攻击会话。
        /// 读档恢复允许把同一业务身份的旧会话重绑到当前版本；live invalidation 则只允许中断。
        /// </summary>
        private static void ReconcileAttackSession(Pawn pawn, bool allowLoadedProjectionRebind)
        {
            if (pawn == null)
            {
                return;
            }

            bool hasBdpJob = HasBdpJob(pawn);
            bool hasBdpBusyStance = HasBdpBusyStance(pawn);
            if (!hasBdpJob && !hasBdpBusyStance)
            {
                return;
            }

            AttackSessionValidationState jobState = hasBdpJob
                ? ValidateCurrentAttackSession(pawn.jobs?.curJob?.verbToUse, allowLoadedProjectionRebind)
                : AttackSessionValidationState.Valid;
            AttackSessionValidationState stanceState = hasBdpBusyStance
                ? ValidateCurrentAttackSession((pawn.stances?.curStance as Stance_Busy)?.verb, allowLoadedProjectionRebind)
                : AttackSessionValidationState.Valid;

            if (jobState == AttackSessionValidationState.Invalid && pawn.jobs?.curJob != null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false, canReturnToPool: false);
            }

            if ((jobState == AttackSessionValidationState.Invalid
                    || stanceState == AttackSessionValidationState.Invalid)
                && pawn.stances != null)
            {
                pawn.stances.CancelBusyStanceHard();
            }
        }

        /// <summary>
        /// 当前 job 是否属于 BDP 攻击会话。
        /// </summary>
        private static bool HasBdpJob(Pawn pawn)
        {
            Job curJob = pawn?.jobs?.curJob;
            if (curJob == null)
            {
                return false;
            }

            return IsBdpFormalHostVerb(curJob.verbToUse)
                || curJob.def == AttackExecutionJobDefs.RangedAttackExecution
                || curJob.def == AttackExecutionJobDefs.MeleeAttackExecution;
        }

        /// <summary>
        /// 当前 BDP job 是否已经失去继续续接的最小真值。
        /// 只有 verb 丢失、HostSessionToken 失效、binding 切换或 cursor 非法时才收口。
        /// </summary>
        private static AttackSessionValidationState ValidateCurrentAttackSession(
            Verb verb,
            bool allowLoadedProjectionRebind)
        {
            if (verb is BdpVerb_FormalHostShoot shootVerb)
            {
                return ValidateRangedFormalHostSession(shootVerb, allowLoadedProjectionRebind);
            }

            if (verb is BdpVerb_FormalHostMelee meleeVerb)
            {
                return ValidateMeleeFormalHostSession(meleeVerb, allowLoadedProjectionRebind);
            }

            return AttackSessionValidationState.Invalid;
        }

        /// <summary>
        /// 校验一条远程 formal host 会话在当前已发布投影下是否仍然可用。
        /// 读档恢复允许把同一业务身份的 loaded 会话重绑到当前投影版本；平时只允许版本严格相等。
        /// </summary>
        private static AttackSessionValidationState ValidateRangedFormalHostSession(
            BdpVerb_FormalHostShoot shootVerb,
            bool allowLoadedProjectionRebind)
        {
            bool shootVerbPresent = shootVerb != null;
            AttackSessionToken token = shootVerbPresent ? shootVerb.HostSessionToken : null;
            bool tokenPresent = token != null;
            bool tokenResultIdPresent = tokenPresent && !string.IsNullOrWhiteSpace(token.ResultId);
            bool resumeChecked = shootVerbPresent && tokenPresent && tokenResultIdPresent;
            bool canResume = resumeChecked && shootVerb.CanResumeLoadedSession();
            if (!shootVerbPresent
                || !tokenPresent
                || !tokenResultIdPresent
                || !canResume)
            {
                return LogValidationAndReturn(
                    shootVerb,
                    allowLoadedProjectionRebind,
                    AttackSessionValidationState.Invalid,
                    "missing_minimum_truth",
                    null,
                    false);
            }

            if (!TryGetPublishedResult(
                    shootVerb.CasterPawn,
                    shootVerb.HostSessionToken.ResultId,
                    out TriggerCombatProjectionState projection,
                    out _,
                    out bool projectionStillPending))
            {
                return LogValidationAndReturn(
                    shootVerb,
                    allowLoadedProjectionRebind,
                    allowLoadedProjectionRebind && projectionStillPending
                        ? AttackSessionValidationState.Deferred
                        : AttackSessionValidationState.Invalid,
                    projectionStillPending ? "published_projection_pending" : "published_result_missing",
                    null,
                    projectionStillPending);
            }

            if (allowLoadedProjectionRebind)
            {
                shootVerb.HostSessionToken = shootVerb.HostSessionToken.WithProjectionVersion(projection.ProjectionVersion);
                return LogValidationAndReturn(
                    shootVerb,
                    allowLoadedProjectionRebind,
                    AttackSessionValidationState.Valid,
                    "projection_rebound",
                    projection,
                    projectionStillPending);
            }

            return LogValidationAndReturn(
                shootVerb,
                allowLoadedProjectionRebind,
                shootVerb.HostSessionToken.ProjectionVersion == projection.ProjectionVersion
                    ? AttackSessionValidationState.Valid
                    : AttackSessionValidationState.Invalid,
                shootVerb.HostSessionToken.ProjectionVersion == projection.ProjectionVersion
                    ? "projection_match"
                    : "projection_mismatch",
                projection,
                projectionStillPending);
        }

        /// <summary>
        /// 当前 stance 是否握着 BDP formal host verb。
        /// </summary>
        private static bool HasBdpBusyStance(Pawn pawn)
        {
            if (!(pawn?.stances?.curStance is Stance_Busy busyStance))
            {
                return false;
            }

            return IsBdpFormalHostVerb(busyStance.verb);
        }

        /// <summary>
        /// 当前忙姿态里的 formal host 会话是否已经失去继续续接的最小真值。
        /// </summary>
        private static AttackSessionValidationState ValidateMeleeFormalHostSession(
            BdpVerb_FormalHostMelee meleeVerb,
            bool allowLoadedProjectionRebind)
        {
            if (meleeVerb == null
                || meleeVerb.HostSessionToken == null
                || string.IsNullOrWhiteSpace(meleeVerb.HostSessionToken.ResultId)
                || !meleeVerb.CanResumeLoadedSession())
            {
                return LogValidationAndReturn(
                    meleeVerb,
                    allowLoadedProjectionRebind,
                    AttackSessionValidationState.Invalid,
                    "missing_minimum_truth",
                    null,
                    false);
            }

            if (!TryGetPublishedResult(
                    meleeVerb.CasterPawn,
                    meleeVerb.HostSessionToken.ResultId,
                    out TriggerCombatProjectionState projection,
                    out _,
                    out bool projectionStillPending))
            {
                return LogValidationAndReturn(
                    meleeVerb,
                    allowLoadedProjectionRebind,
                    allowLoadedProjectionRebind && projectionStillPending
                        ? AttackSessionValidationState.Deferred
                        : AttackSessionValidationState.Invalid,
                    projectionStillPending ? "published_projection_pending" : "published_result_missing",
                    null,
                    projectionStillPending);
            }

            if (allowLoadedProjectionRebind)
            {
                meleeVerb.HostSessionToken = meleeVerb.HostSessionToken.WithProjectionVersion(projection.ProjectionVersion);
                return LogValidationAndReturn(
                    meleeVerb,
                    allowLoadedProjectionRebind,
                    AttackSessionValidationState.Valid,
                    "projection_rebound",
                    projection,
                    projectionStillPending);
            }

            return LogValidationAndReturn(
                meleeVerb,
                allowLoadedProjectionRebind,
                meleeVerb.HostSessionToken.ProjectionVersion == projection.ProjectionVersion
                    ? AttackSessionValidationState.Valid
                    : AttackSessionValidationState.Invalid,
                meleeVerb.HostSessionToken.ProjectionVersion == projection.ProjectionVersion
                    ? "projection_match"
                    : "projection_mismatch",
                projection,
                projectionStillPending);
        }

        /// <summary>
        /// 读取当前 Pawn 的已发布结果，并区分“当前确实无效”和“读档后发布仍未完成”。
        /// </summary>
        private static bool TryGetPublishedResult(
            Pawn pawn,
            string resultId,
            out TriggerCombatProjectionState projection,
            out FormalExpressionResult result,
            out bool projectionStillPending)
        {
            projection = null;
            result = null;
            projectionStillPending = false;
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            if (triggerBody == null)
            {
                return false;
            }

            projectionStillPending = triggerBody.HasPendingPostLoadProjectionRefresh;
            projection = triggerBody.PublishedCombatProjection;
            if (projection == null
                || projection.IsEmpty
                || projection.ProjectionVersion <= 0
                || projection.ResultIndex == null
                || string.IsNullOrWhiteSpace(resultId)
                || !projection.ResultIndex.TryGetValue(resultId, out result)
                || result == null
                || !result.IsAvailable)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断当前 verb 是否属于 BDP internal formal host。
        /// </summary>
        private static bool IsBdpFormalHostVerb(Verb verb)
        {
            return verb is BdpVerb_FormalHostShoot || verb is BdpVerb_FormalHostMelee;
        }

        /// <summary>
        /// 统一记录 formal host 会话校验结果，并把状态原样返回给调用方。
        /// </summary>
        private static AttackSessionValidationState LogValidationAndReturn(
            Verb verb,
            bool allowLoadedProjectionRebind,
            AttackSessionValidationState state,
            string reason,
            TriggerCombatProjectionState projection,
            bool projectionStillPending)
        {
            bool shouldLog = state != AttackSessionValidationState.Valid
                || allowLoadedProjectionRebind
                || reason != "projection_match";
            if (!shouldLog)
            {
                return state;
            }

            AttackSessionToken token = null;
            if (verb is BdpVerb_FormalHostShoot shootVerb)
            {
                token = shootVerb.HostSessionToken;
            }
            else if (verb is BdpVerb_FormalHostMelee meleeVerb)
            {
                token = meleeVerb.HostSessionToken;
            }

            AttackExecutionDiagnostics.LogPostLoadSessionValidation(
                verb?.CasterPawn,
                verb,
                token,
                allowLoadedProjectionRebind,
                state.ToString(),
                reason,
                projection != null ? projection.ProjectionVersion : 0,
                projectionStillPending);
            return state;
        }

        /// <summary>
        /// 当前 formal host 会话校验结果。
        /// Valid 表示可以继续，Invalid 表示必须结束，Deferred 表示应等待读档后的正式发布完成后再决定。
        /// </summary>
        private enum AttackSessionValidationState
        {
            /// <summary>
            /// 当前会话仍然有效。
            /// </summary>
            Valid = 0,

            /// <summary>
            /// 当前会话已经失效，必须结束。
            /// </summary>
            Invalid = 1,

            /// <summary>
            /// 当前还在等待读档后的正式发布完成，暂不做杀会话决定。
            /// </summary>
            Deferred = 2
        }
    }
}
