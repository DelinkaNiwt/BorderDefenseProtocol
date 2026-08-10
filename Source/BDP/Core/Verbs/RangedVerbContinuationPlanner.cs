using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using Verse;

namespace BDP.Core.Verbs
{
    /// <summary>
    /// 远程宿主续射规划器。
    /// 它负责把 follow-up 请求重新接入攻击执行与远程协议，并重建宿主发射计划。
    /// </summary>
    internal sealed class RangedVerbContinuationPlanner
    {
        /// <summary>
        /// 为指定远程宿主重新准备一轮待消费的发射计划。
        /// </summary>
        internal bool TryPreparePendingEmission(
            BdpVerb_Shoot verb,
            LocalTargetInfo sessionTarget,
            AttackExecutionReason reason,
            AttackDispatchIntent dispatchIntent)
        {
            Pawn pawn = verb?.Caster as Pawn;
            if (verb == null
                || pawn == null
                || verb.HostSessionToken == null
                || string.IsNullOrWhiteSpace(verb.HostSessionToken.ResultId)
                || verb.HostSessionToken.ProjectionVersion <= 0)
            {
                return false;
            }

            if (!AttackExecutionPostLoadRecovery.IsCurrentAttackSessionValid(verb))
            {
                return false;
            }

            int savedWindowIndex = verb.EmissionCursor.PendingWindowIndex;
            int savedWindowProjectilePlanIndex = verb.EmissionCursor.PendingWindowProjectilePlanIndex;
            int savedEmissionConsumedCount = verb.EmissionCursor.PendingEmissionConsumedCount;
            bool savedHasCommittedRoundTrion = verb.RoundState.HasCommittedRoundTrion;
            float savedRoundTrionCost = verb.RoundState.CurrentRoundTrionCost;
            float savedRoundMinimumRequired = verb.RoundState.CurrentRoundMinimumRequired;

            AttackExecutionService entry = AttackExecutionSurfaceAccess.ResolveEntry(verb.CasterPawn);
            bool hasDirectHostSession = verb.HostModuleSession != null;
            RangedAttackModuleSession moduleSession = ResolveModuleSession(verb, pawn, out string sessionSource);
            // 续射请求快照优先取宿主保留的完整复合快照。
            // 逐射 dual 的宿主会话是单侧(主手)泳道,直接导出会话会丢副手侧路线引导状态,
            // 导致副手泳道重建时绕行路径丢失、第 2 窗口发射异常、整轮 burst 只出第一发。
            AttackContextSnapshot attackContextSnapshot = verb.HostAttackContextSnapshot != null
                ? verb.HostAttackContextSnapshot
                : AttackExecutionSurfaceAccess.CreateAttackContextSnapshot(moduleSession);
            AttackExecutionDiagnostics.LogContinuationSessionResolved(
                pawn,
                verb,
                verb.HostSessionToken,
                sessionTarget,
                reason,
                dispatchIntent,
                hasDirectHostSession,
                sessionSource,
                moduleSession,
                attackContextSnapshot);
            if (entry == null)
            {
                return false;
            }

            if (moduleSession == null)
            {
                return false;
            }

            AttackExecutionPreparedContext preparedContext;
            if (!entry.TryPreparePlan(
                new AttackExecutionRequest
                {
                    Pawn = pawn,
                    SessionToken = verb.HostSessionToken,
                    AttackContextSnapshot = attackContextSnapshot,
                    Target = sessionTarget,
                    Reason = reason,
                    DispatchIntent = dispatchIntent
                },
                out preparedContext))
            {
                return false;
            }

            RangedAttackExecutionContext context;
            if (!RangedAttackExecutionContext.TryCreateForStep(
                    preparedContext,
                    preparedContext.RuntimeSteps != null && preparedContext.RuntimeSteps.Count > 0 ? preparedContext.RuntimeSteps[0] : null,
                    out context))
            {
                return false;
            }

            RangedAttackProtocolService protocolService = RangedAttackProtocolSurfaceAccess.Resolve(verb.CasterPawn);
            if (!protocolService.TryBuild(preparedContext, context.Step, context.Result, out var protocolResult))
            {
                return false;
            }

            context.BindProtocolResult(protocolResult);
            verb.ApplyExecutionContext(context);
            if (!RangedBurstEmissionAssembler.TryBuild(
                    preparedContext,
                    context,
                    protocolResult,
                    protocolService,
                    out RangedVerbEmissionPlan emissionPlan))
            {
                return false;
            }

            verb.BindVerbEmissionPlan(emissionPlan);
            if (savedHasCommittedRoundTrion)
            {
                verb.RoundState.Restore(savedRoundTrionCost, savedRoundMinimumRequired, true);
            }

            if (!verb.EmissionCursor.TryRestorePreparedEmissionCursor(
                    savedWindowIndex,
                    savedWindowProjectilePlanIndex,
                    savedEmissionConsumedCount))
            {
                verb.ClearPreparedEmissionState();
                return false;
            }

            if (!verb.EmissionCursor.HasPendingEmissionPlan())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 解析当前续射请求应继续复用的模块会话冻结态。
        /// 正常情况下直接复用宿主持有的会话；只有宿主临时丢失时才回落到已发布结果重建。
        /// </summary>
        private static RangedAttackModuleSession ResolveModuleSession(BdpVerb_Shoot verb, Pawn pawn, out string source)
        {
            source = "missing";
            if (verb == null)
            {
                return null;
            }

            if (verb.HostModuleSession != null)
            {
                source = "resident_host";
                return verb.HostModuleSession;
            }

            if (TryCreateSnapshotBackedModuleSession(verb, pawn, out RangedAttackModuleSession snapshotBackedSession))
            {
                source = verb.ResolveEntryModuleSession() != null
                    ? "host_snapshot_over_staged"
                    : "published_result_with_host_snapshot";
                return snapshotBackedSession;
            }

            RangedAttackModuleSession stagedSession = verb.ResolveEntryModuleSession();
            if (stagedSession != null)
            {
                source = "staged_entry";
                return stagedSession;
            }

            if (verb?.HostSessionToken == null || string.IsNullOrWhiteSpace(verb.HostSessionToken.ResultId))
            {
                return null;
            }

            if (AttackExecutionSurfaceAccess.TryCreatePublishedRangedModuleSession(
                pawn,
                verb.HostSessionToken.ResultId,
                out RangedAttackModuleSession moduleSession)
                )
            {
                if (TryApplyHostAttackContextSnapshot(verb, moduleSession))
                {
                    source = "published_result_with_host_snapshot";
                }
                else
                {
                    source = "published_result";
                }

                return moduleSession;
            }

            return null;
        }

        /// <summary>
        /// 基于宿主冻结上下文重建一份模块会话。
        /// 这条路径优先于 staged entry，避免暖机期间误写入的空暂存会话吞掉路线引导状态。
        /// </summary>
        /// <param name="verb">当前远程宿主。</param>
        /// <param name="pawn">当前发射 Pawn。</param>
        /// <param name="moduleSession">重建并灌入宿主上下文后的模块会话。</param>
        /// <returns>成功重建并导入上下文时返回 true。</returns>
        private static bool TryCreateSnapshotBackedModuleSession(
            BdpVerb_Shoot verb,
            Pawn pawn,
            out RangedAttackModuleSession moduleSession)
        {
            moduleSession = null;
            if (verb?.HostAttackContextSnapshot == null
                || verb.HostSessionToken == null
                || string.IsNullOrWhiteSpace(verb.HostSessionToken.ResultId))
            {
                return false;
            }

            if (!AttackExecutionSurfaceAccess.TryCreatePublishedRangedModuleSession(
                    pawn,
                    verb.HostSessionToken.ResultId,
                    out moduleSession))
            {
                return false;
            }

            return TryApplyHostAttackContextSnapshot(verb, moduleSession);
        }

        /// <summary>
        /// 把宿主冻结的攻击上下文重新灌回新建模块会话。
        /// dual 合并入口没有单一宿主会话时，续发必须靠这份快照恢复路线引导等已确认状态。
        /// </summary>
        /// <param name="verb">当前远程宿主。</param>
        /// <param name="moduleSession">刚按已发布结果重建的模块会话。</param>
        /// <returns>成功导入宿主上下文快照时返回 true。</returns>
        private static bool TryApplyHostAttackContextSnapshot(
            BdpVerb_Shoot verb,
            RangedAttackModuleSession moduleSession)
        {
            if (verb?.HostAttackContextSnapshot == null || moduleSession == null)
            {
                return false;
            }

            moduleSession.AttackContext = AttackContext.FromSnapshot(verb.HostAttackContextSnapshot);
            return true;
        }
    }
}
