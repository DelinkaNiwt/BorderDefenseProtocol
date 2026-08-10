using System.Collections.Generic;
using System.Globalization;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Expressions;
using BDP.Core.VerbHosting;
using BDP.Core.Verbs;
using BDP.Support.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// AttackExecution 统一诊断汇总器。
    /// 默认只保留玩家能直接看懂的攻击摘要，以及排查异常时真正有价值的日志。
    /// </summary>
    internal static class AttackExecutionDiagnostics
    {
        /// <summary>
        /// 记录一次正式攻击请求被拒绝。
        /// 这类日志属于异常路径，应长期保留。
        /// </summary>
        public static void LogRejected(AttackExecutionRequest request, string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=attack_reject"
                + ", reason=" + SafeText(reason)
                + ", request={" + DescribeRequest(request) + "}");
        }

        /// <summary>
        /// 记录一次远程攻击正式起手摘要。
        /// 一次攻击只保留这一条概要，不再展开内部每个编排阶段。
        /// </summary>
        public static void LogRangedExecutionStart(RangedAttackExecutionContext context, AttackDispatchIntent dispatchIntent)
        {
            RangedVerbEmissionPlan emissionPlan = context?.ProtocolResult != null
                ? context.ProtocolResult.VerbEmissionPlan
                : null;
            BdpDiagnostics.AttackExecution(
                "event=ranged_start"
                + ", route=" + DescribeRoute(dispatchIntent, context != null && context.RequiresContinuousDriver)
                + ", pawn=" + DescribePawn(context != null ? context.Pawn : null)
                + ", target=" + DescribeTarget(context != null ? context.Target : LocalTargetInfo.Invalid)
                + ", attackId=" + SafeText(context?.Cast != null ? context.Cast.AttackInstanceId : context?.Step != null ? context.Step.AttackInstanceId : null)
                + ", hostResultId=" + SafeText(context != null ? context.HostResultId : null)
                + ", resultId=" + SafeText(context?.Result != null ? context.Result.Id : null)
                + ", windows=" + CountWindows(emissionPlan)
                + ", emits=" + CountExpectedEmits(emissionPlan)
                + ", verb=" + DescribeVerb(context != null ? context.Verb : null));
        }

        /// <summary>
        /// 记录一次近战攻击正式起手摘要。
        /// 近战同样只保留入口摘要，不再拆成内部推进轨迹。
        /// </summary>
        public static void LogMeleeExecutionStart(MeleeAttackExecutionContext context, AttackDispatchIntent dispatchIntent)
        {
            BdpDiagnostics.AttackExecution(
                "event=melee_start"
                + ", route=" + DescribeRoute(dispatchIntent, context != null && context.RequiresContinuousDriver)
                + ", pawn=" + DescribePawn(context != null ? context.Pawn : null)
                + ", target=" + DescribeTarget(context != null ? context.Target : LocalTargetInfo.Invalid)
                + ", attackId=" + SafeText(context?.Cast != null ? context.Cast.AttackInstanceId : context?.Step != null ? context.Step.AttackInstanceId : null)
                + ", resultId=" + SafeText(context?.Result != null ? context.Result.Id : null)
                + ", requiredSteps=" + DescribeRequiredCount(context != null ? context.RequiredStepCount : 0)
                + ", verb=" + DescribeVerb(context != null ? context.Verb : null));
        }

        /// <summary>
        /// 记录一次近战 run 打完后，准备续接下一段前的最小事实。
        /// </summary>
        public static void LogMeleeContinuationPrepare(
            Pawn pawn,
            Verb verb,
            AttackSessionToken token,
            LocalTargetInfo target,
            int nextRuntimeStepIndex)
        {
            BdpDiagnostics.AttackExecution(
                "event=melee_continuation_prepare"
                + ", pawn=" + DescribePawn(pawn)
                + ", target=" + DescribeTarget(target)
                + ", nextStepIndex=" + nextRuntimeStepIndex
                + ", token=" + DescribeToken(token)
                + ", verbState=" + DescribeMeleeVerbState(verb as BdpVerb_MeleeAttackDamage)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次近战正式执行从当前 run 切到下一段 run。
        /// </summary>
        public static void LogMeleeContinuationSwitch(
            Pawn pawn,
            Verb previousVerb,
            Verb nextVerb,
            AttackSessionToken token,
            LocalTargetInfo target,
            int nextRuntimeStepIndex)
        {
            BdpDiagnostics.AttackExecution(
                "event=melee_continuation_switch"
                + ", pawn=" + DescribePawn(pawn)
                + ", target=" + DescribeTarget(target)
                + ", nextStepIndex=" + nextRuntimeStepIndex
                + ", token=" + DescribeToken(token)
                + ", previousState=" + DescribeMeleeVerbState(previousVerb as BdpVerb_MeleeAttackDamage)
                + ", nextState=" + DescribeMeleeVerbState(nextVerb as BdpVerb_MeleeAttackDamage)
                + ", previousVerb=" + DescribeVerb(previousVerb)
                + ", nextVerb=" + DescribeVerb(nextVerb));
        }

        /// <summary>
        /// 记录一次近战 run 续接结束的原因。
        /// 正常打完整轮、状态缺失、或准备失败都会在这里收口。
        /// </summary>
        public static void LogMeleeContinuationEnd(
            Pawn pawn,
            Verb verb,
            AttackSessionToken token,
            int nextRuntimeStepIndex,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=melee_continuation_end"
                + ", pawn=" + DescribePawn(pawn)
                + ", nextStepIndex=" + nextRuntimeStepIndex
                + ", reason=" + SafeText(reason)
                + ", token=" + DescribeToken(token)
                + ", verbState=" + DescribeMeleeVerbState(verb as BdpVerb_MeleeAttackDamage)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次远程宿主发射计划被消费完后的摘要。
        /// 这一条用于确认“这一枪最终打了多少发、是否完整打完”。
        /// </summary>
        public static void LogVerbEmissionSummary(
            Pawn pawn,
            Verb verb,
            RangedVerbEmissionPlan emissionPlan,
            int hostEmissionConsumedCount,
            bool stepCompleted,
            LocalTargetInfo target)
        {
            BdpDiagnostics.AttackExecution(
                "event=ranged_emit_summary"
                + ", pawn=" + DescribePawn(pawn)
                + ", target=" + DescribeTarget(target)
                + ", hostResultId=" + SafeText(emissionPlan != null ? emissionPlan.StepHostResultId : null)
                + ", windows=" + CountWindows(emissionPlan)
                + ", emitted=" + hostEmissionConsumedCount + "/" + CountExpectedEmits(emissionPlan)
                + ", completed=" + stepCompleted
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录远程协议生成的投射计划摘要。
        /// 这里重点展示 sourceResult 与 Launch/Aim/Current 三层目标，方便排查路径模块是否被压平成直射。
        /// </summary>
        public static void LogRangedProjectilePlanSummary(
            Pawn pawn,
            string phase,
            string hostResultId,
            string sourceResultId,
            string attackInstanceId,
            IReadOnlyList<ProjectileInitPlan> projectilePlans)
        {
            BdpDiagnostics.AttackExecution(
                "event=ranged_projectile_plan_summary"
                + ", phase=" + SafeText(phase)
                + ", pawn=" + DescribePawn(pawn)
                + ", attackId=" + SafeText(attackInstanceId)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", sourceResultId=" + SafeText(sourceResultId)
                + ", planCount=" + CountItems(projectilePlans)
                + ", plans=" + DescribeProjectilePlans(projectilePlans));
            BdpDiagnostics.AttackExecution(
                "event=target_semantics_projectile_plan"
                + ", phase=" + SafeText(phase)
                + ", pawn=" + DescribePawn(pawn)
                + ", attackId=" + SafeText(attackInstanceId)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", sourceResultId=" + SafeText(sourceResultId)
                + ", planCount=" + CountItems(projectilePlans)
                + ", semantics=" + DescribeProjectilePlanSemantics(projectilePlans));
        }

        /// <summary>
        /// 记录投射物飞行中实时目标语义的变更。
        /// 这条日志只展示 Live 层变化后的事实，不反向解释业务模块意图。
        /// </summary>
        public static void LogTargetSemanticsLiveUpdate(
            Thing projectile,
            ProjectileInitPlan plan,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=target_semantics_live_update"
                + ", reason=" + SafeText(reason)
                + ", projectile=" + SafeText(projectile != null ? projectile.ThingID : null)
                + ", attackId=" + SafeText(plan != null ? plan.AttackInstanceId : null)
                + ", resultId=" + SafeText(plan != null ? plan.ResultId : null)
                + ", emitIndex=" + (plan != null ? plan.EmitIndex.ToString() : "-1")
                + ", targetSemantics=" + DescribeTargetSemantics(plan != null ? plan.TargetSemantics : null));
        }

        /// <summary>
        /// 记录一次在新起手前清理掉的旧发射计划。
        /// 只在旧计划目标与新请求目标不一致时输出，用来排查“第一枪打偏旧目标”这类异常。
        /// </summary>
        public static void LogStalePendingEmissionPlanCleared(
            Pawn pawn,
            Verb verb,
            RangedVerbEmissionPlan emissionPlan,
            int pendingWindowIndex,
            int pendingWindowProjectilePlanIndex,
            int pendingEmissionConsumedCount,
            LocalTargetInfo previousTarget,
            LocalTargetInfo requestedTarget)
        {
            BdpDiagnostics.AttackExecution(
                "event=stale_pending_plan_cleared"
                + ", pawn=" + DescribePawn(pawn)
                + ", previousTarget=" + DescribeTarget(previousTarget)
                + ", requestedTarget=" + DescribeTarget(requestedTarget)
                + ", hostResultId=" + SafeText(emissionPlan != null ? emissionPlan.StepHostResultId : null)
                + ", windows=" + CountWindows(emissionPlan)
                + ", emitted=" + pendingEmissionConsumedCount + "/" + CountExpectedEmits(emissionPlan)
                + ", cursor=" + pendingWindowIndex + ":" + pendingWindowProjectilePlanIndex
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次“请求目标”和“准备好的首发目标”不一致的异常。
        /// 这条日志不判断业务是否一定错误，只提供最小证据帮助定位。
        /// </summary>
        public static void LogPreparedTargetMismatch(
            Pawn pawn,
            Verb verb,
            string hostResultId,
            LocalTargetInfo requestedTarget,
            LocalTargetInfo preparedTarget)
        {
            BdpDiagnostics.AttackExecution(
                "event=prepared_target_mismatch"
                + ", pawn=" + DescribePawn(pawn)
                + ", requestedTarget=" + DescribeTarget(requestedTarget)
                + ", preparedTarget=" + DescribeTarget(preparedTarget)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次 formal host 的正式绑定变化。
        /// 只在结果标识真的发生切换时输出，用来观察当前壳到底绑到了谁。
        /// </summary>
        public static void LogFormalHostRebind(
            BdpFormalVerbHostSlot slot,
            WeaponExpressionMode weaponMode,
            string loadId,
            string previousResultId,
            string nextResultId,
            bool resetApplied)
        {
            BdpDiagnostics.AttackExecution(
                "event=formal_host_rebind"
                + ", slot=" + slot
                + ", mode=" + weaponMode
                + ", loadId=" + SafeText(loadId)
                + ", previousResultId=" + SafeText(previousResultId)
                + ", nextResultId=" + SafeText(nextResultId)
                + ", reset=" + resetApplied);
        }

        /// <summary>
        /// 记录一次 formal host 因 binding 表面变化而执行 reset 前的边界状态。
        /// 它专门用于排查“结果没换，但近战续接状态被清空”的问题。
        /// </summary>
        public static void LogFormalHostBindingReset(
            Pawn pawn,
            Verb verb,
            BdpFormalVerbHostSlot slot,
            WeaponExpressionMode weaponMode,
            string loadId,
            string previousResultId,
            string nextResultId,
            AttackSessionToken previousHostToken,
            AttackSessionToken previousPlanToken,
            int nextRuntimeStepIndex,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=formal_host_binding_reset"
                + ", pawn=" + DescribePawn(pawn)
                + ", slot=" + slot
                + ", mode=" + weaponMode
                + ", loadId=" + SafeText(loadId)
                + ", previousResultId=" + SafeText(previousResultId)
                + ", nextResultId=" + SafeText(nextResultId)
                + ", previousHostToken=" + DescribeToken(previousHostToken)
                + ", previousPlanToken=" + DescribeToken(previousPlanToken)
                + ", nextStepIndex=" + nextRuntimeStepIndex
                + ", reason=" + SafeText(reason)
                + ", verbState=" + DescribeMeleeVerbState(verb as BdpVerb_MeleeAttackDamage)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录 formal host 从 binding 同步会话令牌的边界。
        /// 只在 reset、令牌变化或令牌缺失时由调用方触发，用于追踪会话真值何时被覆盖成 null。
        /// </summary>
        public static void LogFormalHostSessionTokenSync(
            Pawn pawn,
            Verb verb,
            BdpFormalVerbHostSlot slot,
            string loadId,
            string previousResultId,
            string nextResultId,
            AttackSessionToken previousToken,
            AttackSessionToken bindingToken,
            AttackSessionToken finalToken,
            bool resetApplied,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=formal_host_session_token_sync"
                + ", pawn=" + DescribePawn(pawn)
                + ", slot=" + slot
                + ", loadId=" + SafeText(loadId)
                + ", previousResultId=" + SafeText(previousResultId)
                + ", nextResultId=" + SafeText(nextResultId)
                + ", previousToken=" + DescribeToken(previousToken)
                + ", bindingToken=" + DescribeToken(bindingToken)
                + ", finalToken=" + DescribeToken(finalToken)
                + ", reset=" + resetApplied
                + ", reason=" + SafeText(reason)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录 BDP 远程宿主会话真值被清空的边界。
        /// 这条日志只服务“不知道谁把 token 清掉了”的取证，不参与任何判定。
        /// </summary>
        public static void LogVerbSessionCleared(
            Pawn pawn,
            Verb verb,
            AttackSessionToken previousToken,
            string previousAttackInstanceId,
            string previousResultId,
            LocalTargetInfo previousSessionTarget,
            bool hadPendingPlan,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=verb_session_cleared"
                + ", pawn=" + DescribePawn(pawn)
                + ", reason=" + SafeText(reason)
                + ", previousToken=" + DescribeToken(previousToken)
                + ", previousAttackId=" + SafeText(previousAttackInstanceId)
                + ", previousResultId=" + SafeText(previousResultId)
                + ", previousSessionTarget=" + DescribeTarget(previousSessionTarget)
                + ", hadPendingPlan=" + hadPendingPlan
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录 BDP 近战宿主会话与续接状态被清空的边界。
        /// 它用于确认同一实例内的后续 step 是否在 reset 前就被抹掉了。
        /// </summary>
        public static void LogMeleeVerbSessionCleared(
            Pawn pawn,
            Verb verb,
            AttackSessionToken previousHostToken,
            AttackSessionToken previousPlanToken,
            string previousAttackInstanceId,
            string previousResultId,
            int nextRuntimeStepIndex,
            AttackDispatchIntent dispatchIntent,
            AttackExecutionReason planReason,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=melee_verb_session_cleared"
                + ", pawn=" + DescribePawn(pawn)
                + ", reason=" + SafeText(reason)
                + ", previousHostToken=" + DescribeToken(previousHostToken)
                + ", previousPlanToken=" + DescribeToken(previousPlanToken)
                + ", previousAttackId=" + SafeText(previousAttackInstanceId)
                + ", previousResultId=" + SafeText(previousResultId)
                + ", nextStepIndex=" + nextRuntimeStepIndex
                + ", hadPendingContinuation=" + (nextRuntimeStepIndex >= 0)
                + ", dispatchIntent=" + dispatchIntent
                + ", planReason=" + planReason
                + ", verbState=" + DescribeMeleeVerbState(verb as BdpVerb_MeleeAttackDamage)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录持续远程 job 因 formal host 会话校验失败而中断前的边界状态。
        /// </summary>
        public static void LogRangedJobSessionInvalid(
            Pawn pawn,
            Verb verb,
            LocalTargetInfo target,
            int castCount,
            int requiredCastCount,
            AttackSessionToken token,
            string verbState)
        {
            BdpDiagnostics.AttackExecution(
                "event=ranged_job_session_invalid"
                + ", pawn=" + DescribePawn(pawn)
                + ", target=" + DescribeTarget(target)
                + ", castCount=" + castCount + "/" + requiredCastCount
                + ", token=" + DescribeToken(token)
                + ", verbState=" + SafeText(verbState)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录持续远程 job 在退出 cleanup 处到底是“执行 reset”还是“按代际跳过 reset”。
        /// 它只用于边界取证，不进入 tick 热路径。
        /// </summary>
        public static void LogRangedJobCleanupDecision(
            Pawn pawn,
            Verb verb,
            AttackSessionToken ownedToken,
            AttackSessionToken currentToken,
            JobCondition condition,
            bool willReset,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=ranged_job_cleanup_decision"
                + ", pawn=" + DescribePawn(pawn)
                + ", condition=" + condition
                + ", ownedToken=" + DescribeToken(ownedToken)
                + ", currentToken=" + DescribeToken(currentToken)
                + ", willReset=" + willReset
                + ", reason=" + SafeText(reason)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录 entry session 解析时 resident 与 staged 同时存在的冲突现场。
        /// 这条日志只在冲突时由调用方触发，并做短间隔节流，避免原版查询链刷屏。
        /// </summary>
        public static void LogEntryModuleSessionResolution(
            Pawn pawn,
            Verb verb,
            AttackSessionToken token,
            RangedAttackModuleSession residentSession,
            RangedAttackModuleSession stagedSession,
            string selectedSource,
            string reason)
        {
            string key = "entry_module_session_resolution."
                + SafeText(pawn != null ? pawn.ThingID : null)
                + "."
                + SafeText(verb != null ? verb.loadID : null)
                + "."
                + SafeText(selectedSource)
                + "."
                + SafeText(reason);
            BdpDiagnostics.AttackExecutionThrottled(
                key,
                "event=entry_module_session_resolution"
                + ", pawn=" + DescribePawn(pawn)
                + ", selected=" + SafeText(selectedSource)
                + ", reason=" + SafeText(reason)
                + ", token=" + DescribeToken(token)
                + ", residentSession={" + DescribeModuleSession(residentSession) + "}"
                + ", stagedSession={" + DescribeModuleSession(stagedSession) + "}"
                + ", verb=" + DescribeVerb(verb),
                30);
        }

        /// <summary>
        /// 记录 entry staging 表面被清空。
        /// 用它确认自动入口暂存态是否在正式提交或 reset 后及时退出。
        /// </summary>
        public static void LogEntryModuleSessionCleared(
            Pawn pawn,
            Verb verb,
            AttackSessionToken token,
            RangedAttackModuleSession stagedSession,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=entry_module_session_cleared"
                + ", pawn=" + DescribePawn(pawn)
                + ", reason=" + SafeText(reason)
                + ", token=" + DescribeToken(token)
                + ", stagedSession={" + DescribeModuleSession(stagedSession) + "}"
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录 dual 远程编排开始时解析到的复合来源和目标事实。
        /// </summary>
        public static void LogDualRangedPlanStart(
            Pawn pawn,
            string hostResultId,
            string mainSourceResultId,
            string subSourceResultId,
            LocalTargetInfo requestTarget,
            LocalTargetInfo semanticTarget)
        {
            BdpDiagnostics.AttackExecution(
                "event=dual_ranged_plan_start"
                + ", pawn=" + DescribePawn(pawn)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", mainSourceResultId=" + SafeText(mainSourceResultId)
                + ", subSourceResultId=" + SafeText(subSourceResultId)
                + ", requestTarget=" + DescribeTarget(requestTarget)
                + ", semanticTarget=" + DescribeTarget(semanticTarget));
        }

        /// <summary>
        /// 记录 dual 近战编排开始时解析到的复合来源和目标事实。
        /// </summary>
        public static void LogDualMeleePlanStart(
            Pawn pawn,
            string hostResultId,
            string mainSourceResultId,
            string subSourceResultId,
            LocalTargetInfo requestTarget,
            LocalTargetInfo semanticTarget)
        {
            BdpDiagnostics.AttackExecution(
                "event=dual_melee_plan_start"
                + ", pawn=" + DescribePawn(pawn)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", mainSourceResultId=" + SafeText(mainSourceResultId)
                + ", subSourceResultId=" + SafeText(subSourceResultId)
                + ", requestTarget=" + DescribeTarget(requestTarget)
                + ", semanticTarget=" + DescribeTarget(semanticTarget));
        }

        /// <summary>
        /// 记录 dual 远程每一侧在编排准入处的必要直射裁定。
        /// requiresVerbLos 是原始 Verb LOS 事实；requiresDirectTargetLos 才是 dual 分侧裁剪真值。
        /// </summary>
        public static void LogDualRangedSideLegality(
            Pawn pawn,
            string hostResultId,
            string side,
            FormalExpressionResult result,
            LocalTargetInfo semanticTarget,
            bool requiresVerbLos,
            bool requiresDirectTargetLos,
            bool hasBinding,
            Verb verb,
            bool canHitDirectTarget,
            bool allowed,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=dual_ranged_side_legality"
                + ", pawn=" + DescribePawn(pawn)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", side=" + SafeText(side)
                + ", result={" + DescribeResult(result) + "}"
                + ", semanticTarget=" + DescribeTarget(semanticTarget)
                + ", requiresVerbLos=" + requiresVerbLos
                + ", requiresDirectTargetLos=" + requiresDirectTargetLos
                + ", hasBinding=" + hasBinding
                + ", canHitDirectTarget=" + canHitDirectTarget
                + ", allowed=" + allowed
                + ", reason=" + SafeText(reason)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录 dual 近战每一侧在编排准入处的目标合法性裁定。
        /// 近战准入只回答“能否进入近战追击链”，不要求当前站位已经贴到目标。
        /// </summary>
        public static void LogDualMeleeSideLegality(
            Pawn pawn,
            string hostResultId,
            string side,
            FormalExpressionResult result,
            LocalTargetInfo semanticTarget,
            bool hasBinding,
            bool hasThingTarget,
            bool allowed,
            string reason,
            Verb verb)
        {
            BdpDiagnostics.AttackExecution(
                "event=dual_melee_side_legality"
                + ", pawn=" + DescribePawn(pawn)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", side=" + SafeText(side)
                + ", result={" + DescribeResult(result) + "}"
                + ", semanticTarget=" + DescribeTarget(semanticTarget)
                + ", hasBinding=" + hasBinding
                + ", hasThingTarget=" + hasThingTarget
                + ", allowed=" + allowed
                + ", reason=" + SafeText(reason)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录 manual targeting 阶段 dual 每一侧的即时合法性裁定。
        /// 这条日志用于区分“入口被直射规则挡住”和“模块确认阶段拒绝”。
        /// </summary>
        public static void LogManualDualTargetingSideLegality(
            Pawn pawn,
            string hostResultId,
            string sourceResultId,
            LocalTargetInfo target,
            bool useValidateTarget,
            bool resolved,
            bool requiresDirectTargetLos,
            bool allowed,
            string reason,
            Verb verb)
        {
            string key = "manual_dual_targeting_side_legality:"
                + SafeText(pawn != null ? pawn.ThingID : null)
                + ":" + SafeText(hostResultId)
                + ":" + SafeText(sourceResultId)
                + ":" + target.Cell
                + ":" + useValidateTarget
                + ":" + SafeText(reason);
            BdpDiagnostics.AttackExecutionThrottled(
                key,
                "event=manual_dual_targeting_side_legality"
                + ", pawn=" + DescribePawn(pawn)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", sourceResultId=" + SafeText(sourceResultId)
                + ", target=" + DescribeTarget(target)
                + ", useValidateTarget=" + useValidateTarget
                + ", resolved=" + resolved
                + ", requiresDirectTargetLos=" + requiresDirectTargetLos
                + ", allowed=" + allowed
                + ", reason=" + SafeText(reason)
                + ", verb=" + DescribeVerb(verb),
                60);
        }

        /// <summary>
        /// 记录组级手动 targeting 是否正确承接成员的多步续选状态。
        /// </summary>
        public static void LogGroupedManualTargetingContinuation(
            Pawn pawn,
            int sourceCount,
            string sourceResultIds,
            bool hasContinuation,
            string phase)
        {
            BdpDiagnostics.AttackExecutionThrottled(
                "grouped_manual_targeting_continuation:"
                + SafeText(pawn != null ? pawn.ThingID : null)
                + ":" + SafeText(sourceResultIds)
                + ":" + SafeText(phase)
                + ":" + hasContinuation,
                "event=grouped_manual_targeting_continuation"
                + ", pawn=" + DescribePawn(pawn)
                + ", sourceCount=" + sourceCount
                + ", sourceResultIds=" + SafeText(sourceResultIds)
                + ", hasContinuation=" + hasContinuation
                + ", phase=" + SafeText(phase),
                30);
        }

        /// <summary>
        /// 记录 dual 远程编排裁剪后的最终结果。
        /// </summary>
        public static void LogDualRangedPlanResult(
            Pawn pawn,
            string hostResultId,
            LocalTargetInfo semanticTarget,
            int survivorCount,
            string survivorResultIds,
            int castCount,
            string outcome)
        {
            BdpDiagnostics.AttackExecution(
                "event=dual_ranged_plan_result"
                + ", pawn=" + DescribePawn(pawn)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", semanticTarget=" + DescribeTarget(semanticTarget)
                + ", survivorCount=" + survivorCount
                + ", survivorResultIds=" + SafeText(survivorResultIds)
                + ", castCount=" + castCount
                + ", outcome=" + SafeText(outcome));
        }

        /// <summary>
        /// 记录 dual 近战编排裁剪后的最终结果。
        /// </summary>
        public static void LogDualMeleePlanResult(
            Pawn pawn,
            string hostResultId,
            LocalTargetInfo semanticTarget,
            int survivorCount,
            string survivorResultIds,
            int castCount,
            string outcome)
        {
            BdpDiagnostics.AttackExecution(
                "event=dual_melee_plan_result"
                + ", pawn=" + DescribePawn(pawn)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", semanticTarget=" + DescribeTarget(semanticTarget)
                + ", survivorCount=" + survivorCount
                + ", survivorResultIds=" + SafeText(survivorResultIds)
                + ", castCount=" + castCount
                + ", outcome=" + SafeText(outcome));
        }

        /// <summary>
        /// 记录原版自动链路对 dual 复合 formal host 的 LOS 探测。
        /// 这个边界可能被原版每 tick 反复询问，因此必须节流。
        /// </summary>
        public static void LogDualRangedHostLosProbe(
            Pawn pawn,
            string hostResultId,
            IntVec3 root,
            LocalTargetInfo target,
            bool effectiveCanHit,
            bool baseCanHit,
            string mainSourceResultId,
            bool mainAllowed,
            string subSourceResultId,
            bool subAllowed,
            string reason)
        {
            string key = "dual_host_los_probe:"
                + SafeText(pawn != null ? pawn.ThingID : null)
                + ":" + SafeText(hostResultId)
                + ":" + root
                + ":" + DescribeTarget(target)
                + ":" + effectiveCanHit
                + ":" + baseCanHit
                + ":" + mainAllowed
                + ":" + subAllowed;
            BdpDiagnostics.AttackExecutionThrottled(
                key,
                "event=dual_ranged_host_los_probe"
                + ", pawn=" + DescribePawn(pawn)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", root=" + root
                + ", target=" + DescribeTarget(target)
                + ", effectiveCanHit=" + effectiveCanHit
                + ", baseCanHit=" + baseCanHit
                + ", mainSourceResultId=" + SafeText(mainSourceResultId)
                + ", mainAllowed=" + mainAllowed
                + ", subSourceResultId=" + SafeText(subSourceResultId)
                + ", subAllowed=" + subAllowed
                + ", reason=" + SafeText(reason),
                120);
        }

        /// <summary>
        /// 记录一次 continuation 最终实际消费的模块会话来源。
        /// 关键在于看它吃的是 resident host、staged entry，还是 published fallback。
        /// </summary>
        public static void LogContinuationSessionResolved(
            Pawn pawn,
            Verb verb,
            AttackSessionToken token,
            LocalTargetInfo target,
            AttackExecutionReason reason,
            AttackDispatchIntent dispatchIntent,
            bool hasDirectHostSession,
            string sessionSource,
            RangedAttackModuleSession moduleSession,
            AttackContextSnapshot attackContextSnapshot)
        {
            BdpDiagnostics.AttackExecution(
                "event=continuation_session_resolved"
                + ", pawn=" + DescribePawn(pawn)
                + ", target=" + DescribeTarget(target)
                + ", reason=" + reason
                + ", dispatch=" + dispatchIntent
                + ", directHostSession=" + hasDirectHostSession
                + ", source=" + SafeText(sessionSource)
                + ", token=" + DescribeToken(token)
                + ", moduleSession={" + DescribeModuleSession(moduleSession) + "}"
                + ", requestSnapshot={" + DescribeAttackContextSnapshot(attackContextSnapshot) + "}"
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次正式执行上下文被绑定到宿主壳时的快照。
        /// 用它确认宿主此刻拿到的到底是完整会话还是空壳。
        /// </summary>
        public static void LogHostSessionBound(
            Pawn pawn,
            Verb verb,
            AttackSessionToken token,
            string hostResultId,
            LocalTargetInfo target,
            RangedAttackModuleSession moduleSession)
        {
            BdpDiagnostics.AttackExecution(
                "event=host_session_bound"
                + ", pawn=" + DescribePawn(pawn)
                + ", target=" + DescribeTarget(target)
                + ", hostResultId=" + SafeText(hostResultId)
                + ", token=" + DescribeToken(token)
                + ", moduleSession={" + DescribeModuleSession(moduleSession) + "}"
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次读档后 / live invalidation 的 formal host 会话校验结果。
        /// 用它确认“坏掉”的判定是发生在最小真值缺失、发布结果缺失，还是版本不一致。
        /// </summary>
        public static void LogPostLoadSessionValidation(
            Pawn pawn,
            Verb verb,
            AttackSessionToken token,
            bool allowLoadedProjectionRebind,
            string state,
            string reason,
            int publishedProjectionVersion,
            bool projectionStillPending)
        {
            BdpDiagnostics.AttackExecution(
                "event=postload_session_validation"
                + ", pawn=" + DescribePawn(pawn)
                + ", mode=" + (allowLoadedProjectionRebind ? "allow_rebind" : "strict")
                + ", state=" + SafeText(state)
                + ", reason=" + SafeText(reason)
                + ", publishedProjection=" + publishedProjectionVersion
                + ", projectionStillPending=" + projectionStillPending
                + ", token=" + DescribeToken(token)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次宿主真正尝试起手前的边界条件。
        /// 这条日志同时服务自动和手动链路，帮助确认“看起来能打”时内部究竟认为哪些条件成立。
        /// </summary>
        public static void LogVerbCastAttempt(
            Pawn pawn,
            Verb verb,
            AttackSessionToken token,
            AttackExecutionReason reason,
            AttackDispatchIntent dispatchIntent,
            LocalTargetInfo requestedTarget,
            LocalTargetInfo sessionTarget,
            LocalTargetInfo currentTarget,
            LocalTargetInfo preparedLaunchTarget,
            LocalTargetInfo preparedAimTarget,
            LocalTargetInfo preparedCurrentTarget,
            bool canHitTarget,
            bool canHitTargetFromCurrentPos,
            bool hasShootLineToRequestedTarget,
            bool hasShootLineToPreparedLaunchTarget,
            bool hasPendingPlan,
            int remainingWindows,
            int remainingProjectiles)
        {
            BdpDiagnostics.AttackExecution(
                "event=verb_cast_attempt"
                + ", pawn=" + DescribePawn(pawn)
                + ", reason=" + reason
                + ", dispatch=" + dispatchIntent
                + ", requestedTarget=" + DescribeTarget(requestedTarget)
                + ", sessionTarget=" + DescribeTarget(sessionTarget)
                + ", currentTarget=" + DescribeTarget(currentTarget)
                + ", preparedLaunchTarget=" + DescribeTarget(preparedLaunchTarget)
                + ", preparedAimTarget=" + DescribeTarget(preparedAimTarget)
                + ", preparedCurrentTarget=" + DescribeTarget(preparedCurrentTarget)
                + ", canHitTarget=" + canHitTarget
                + ", canHitFromCurrentPos=" + canHitTargetFromCurrentPos
                + ", hasShootLineToRequested=" + hasShootLineToRequestedTarget
                + ", hasShootLineToPreparedLaunch=" + hasShootLineToPreparedLaunchTarget
                + ", hasPendingPlan=" + hasPendingPlan
                + ", remainingWindows=" + remainingWindows
                + ", remainingProjectiles=" + remainingProjectiles
                + ", token=" + DescribeToken(token)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次宿主起手调用的结果。
        /// 重点看它死在 prepare、trion、base.TryStartCastOn 还是已经成功挂上 warmup。
        /// </summary>
        public static void LogVerbCastResult(
            Pawn pawn,
            Verb verb,
            AttackSessionToken token,
            AttackExecutionReason reason,
            AttackDispatchIntent dispatchIntent,
            string outcome,
            LocalTargetInfo requestedTarget,
            LocalTargetInfo preparedLaunchTarget,
            LocalTargetInfo preparedAimTarget,
            LocalTargetInfo preparedCurrentTarget,
            bool started,
            bool warmingUp,
            bool bursting,
            bool fullBodyBusy,
            string verbState,
            bool hasPendingPlan,
            int remainingWindows,
            int remainingProjectiles)
        {
            BdpDiagnostics.AttackExecution(
                "event=verb_cast_result"
                + ", pawn=" + DescribePawn(pawn)
                + ", reason=" + reason
                + ", dispatch=" + dispatchIntent
                + ", outcome=" + SafeText(outcome)
                + ", requestedTarget=" + DescribeTarget(requestedTarget)
                + ", preparedLaunchTarget=" + DescribeTarget(preparedLaunchTarget)
                + ", preparedAimTarget=" + DescribeTarget(preparedAimTarget)
                + ", preparedCurrentTarget=" + DescribeTarget(preparedCurrentTarget)
                + ", started=" + started
                + ", warmingUp=" + warmingUp
                + ", bursting=" + bursting
                + ", fullBodyBusy=" + fullBodyBusy
                + ", verbState=" + SafeText(verbState)
                + ", hasPendingPlan=" + hasPendingPlan
                + ", remainingWindows=" + remainingWindows
                + ", remainingProjectiles=" + remainingProjectiles
                + ", token=" + DescribeToken(token)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次手动持续推进 job 即将调用 Verb 起手前的条件。
        /// </summary>
        public static void LogContinuousJobCastAttempt(
            Pawn pawn,
            Verb verb,
            LocalTargetInfo target,
            int castCount,
            int requiredCastCount,
            bool canHitCurrentTarget,
            bool canHitTargetFromCurrentPos)
        {
            BdpDiagnostics.AttackExecution(
                "event=continuous_job_cast_attempt"
                + ", pawn=" + DescribePawn(pawn)
                + ", target=" + DescribeTarget(target)
                + ", castCount=" + castCount + "/" + requiredCastCount
                + ", canHitCurrentTarget=" + canHitCurrentTarget
                + ", canHitFromCurrentPos=" + canHitTargetFromCurrentPos
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 记录一次手动持续推进 job 调用 Verb 起手后的结果。
        /// </summary>
        public static void LogContinuousJobCastResult(
            Pawn pawn,
            Verb verb,
            LocalTargetInfo target,
            int castCount,
            int requiredCastCount,
            string outcome,
            bool started,
            bool canHitTargetFromCurrentPos,
            bool endIfCantShootTargetFromCurPos,
            string verbState)
        {
            BdpDiagnostics.AttackExecution(
                "event=continuous_job_cast_result"
                + ", pawn=" + DescribePawn(pawn)
                + ", target=" + DescribeTarget(target)
                + ", castCount=" + castCount + "/" + requiredCastCount
                + ", outcome=" + SafeText(outcome)
                + ", started=" + started
                + ", canHitFromCurrentPos=" + canHitTargetFromCurrentPos
                + ", endIfCantShootTargetFromCurPos=" + endIfCantShootTargetFromCurPos
                + ", verbState=" + SafeText(verbState)
                + ", verb=" + DescribeVerb(verb));
        }

        /// <summary>
        /// 将外部派单意图压成玩家更容易读懂的路由标签。
        /// </summary>
        private static string DescribeRoute(AttackDispatchIntent dispatchIntent, bool requiresContinuousDriver)
        {
            if (dispatchIntent == AttackDispatchIntent.ForceTargetOrder)
            {
                return "force_order";
            }

            if (dispatchIntent == AttackDispatchIntent.AutoAttackOrder)
            {
                return "auto_order";
            }

            return requiresContinuousDriver ? "continuous" : "immediate";
        }

        /// <summary>
        /// 输出请求摘要。
        /// </summary>
        private static string DescribeRequest(AttackExecutionRequest request)
        {
            if (request == null)
            {
                return "null";
            }

            return "pawn=" + DescribePawn(request.Pawn)
                + ", attackId=" + SafeText(request.AttackInstanceId)
                + ", resultId=" + SafeText(request.ResultId)
                + ", target=" + DescribeTarget(request.Target)
                + ", reason=" + request.Reason
                + ", dispatch=" + request.DispatchIntent;
        }

        /// <summary>
        /// 输出 Pawn 摘要。
        /// </summary>
        private static string DescribePawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return "null";
            }

            return SafeText(pawn.LabelShortCap) + "(" + pawn.ThingID + ")";
        }

        /// <summary>
        /// 输出目标摘要。
        /// </summary>
        private static string DescribeTarget(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "invalid";
            }

            if (target.HasThing)
            {
                return SafeText(target.Thing.LabelShortCap) + "(" + target.Thing.ThingID + ")" + "@" + target.Cell;
            }

            return target.Cell.ToString();
        }

        /// <summary>
        /// 输出 Verb 摘要。
        /// </summary>
        private static string DescribeVerb(Verb verb)
        {
            if (verb == null)
            {
                return "null";
            }

            VerbProperties props = verb.verbProps;
            return verb.GetType().Name
                + "[label=" + SafeText(props != null ? props.label : null)
                + ", loadId=" + SafeText(verb.loadID)
                + ", burst=" + (props != null ? props.burstShotCount : 0)
                + "]";
        }

        /// <summary>
        /// 输出正式结果摘要。
        /// </summary>
        private static string DescribeResult(FormalExpressionResult result)
        {
            if (result == null)
            {
                return "null";
            }

            ResolvedVerbSpec resolvedSpec = result.ResolvedVerbSpec;
            bool requiresVerbLos = resolvedSpec != null
                ? resolvedSpec.RequireLineOfSight
                : result.VerbProps != null && result.VerbProps.requireLineOfSight;
            return "id=" + SafeText(result.Id)
                + ", mode=" + result.WeaponMode
                + ", available=" + result.IsAvailable
                + ", requiresVerbLos=" + requiresVerbLos
                + ", requiresDirectTargetLos=" + (resolvedSpec != null && resolvedSpec.RequiresDirectTargetLineOfSight)
                + ", label=" + SafeText(result.DisplayLabel);
        }

        /// <summary>
        /// 输出投射计划集合摘要。
        /// </summary>
        private static string DescribeProjectilePlans(IReadOnlyList<ProjectileInitPlan> plans)
        {
            if (plans == null || plans.Count == 0)
            {
                return "[]";
            }

            List<string> entries = new List<string>();
            for (int i = 0; i < plans.Count; i++)
            {
                entries.Add(DescribeProjectilePlan(i, plans[i]));
            }

            return "[" + string.Join("; ", entries.ToArray()) + "]";
        }

        /// <summary>
        /// 输出单条投射计划摘要。
        /// </summary>
        private static string DescribeProjectilePlan(int index, ProjectileInitPlan plan)
        {
            if (plan == null)
            {
                return "#" + index + "{null}";
            }

            return "#" + index
                + "{planSourceResultId=" + SafeText(plan.ResultId)
                + ", planEmitIndex=" + plan.EmitIndex
                + ", planLaunchTarget=" + DescribeTarget(plan.LaunchTarget)
                + ", planAimTarget=" + DescribeTarget(plan.AimTarget)
                + ", planCurrentTarget=" + DescribeTarget(plan.CurrentTarget)
                + ", targetSemantics=" + DescribeTargetSemantics(plan.TargetSemantics)
                + "}";
        }

        /// <summary>
        /// 输出投射计划集合中的目标语义摘要。
        /// </summary>
        private static string DescribeProjectilePlanSemantics(IReadOnlyList<ProjectileInitPlan> plans)
        {
            if (plans == null || plans.Count == 0)
            {
                return "[]";
            }

            List<string> entries = new List<string>();
            for (int i = 0; i < plans.Count; i++)
            {
                entries.Add("#" + i + "{" + DescribeTargetSemantics(plans[i]?.TargetSemantics) + "}");
            }

            return "[" + string.Join("; ", entries.ToArray()) + "]";
        }

        /// <summary>
        /// 输出单发投射物的完整目标语义。
        /// </summary>
        private static string DescribeTargetSemantics(RangedProjectileTargetSemantics semantics)
        {
            if (semantics == null)
            {
                return "null";
            }

            return "intentFinalTarget=" + DescribeTarget(semantics.IntentFinalTarget)
                + ", intentFinalPoint=" + DescribeVector(semantics.IntentFinalPoint)
                + ", intentFirstTarget=" + DescribeTarget(semantics.IntentFirstTarget)
                + ", intentFirstPoint=" + DescribeVector(semantics.IntentFirstPoint)
                + ", liveFinalTarget=" + DescribeTarget(semantics.LiveFinalTarget)
                + ", liveFinalPoint=" + DescribeVector(semantics.LiveFinalPoint)
                + ", liveNextTarget=" + DescribeTarget(semantics.LiveNextTarget)
                + ", liveNextPoint=" + DescribeVector(semantics.LiveNextPoint);
        }

        /// <summary>
        /// 输出宿主会话令牌摘要。
        /// </summary>
        private static string DescribeToken(AttackSessionToken token)
        {
            if (token == null)
            {
                return "null";
            }

            return "attackId=" + SafeText(token.AttackInstanceId)
                + "/resultId=" + SafeText(token.ResultId)
                + "/projection=" + token.ProjectionVersion
                + "/owner=" + SafeText(token.OwnerPawnThingId);
        }

        /// <summary>
        /// 输出近战宿主当前挂着的正式会话与续接状态摘要。
        /// 它只在诊断关键边界日志里使用，避免把常规日志刷得过长。
        /// </summary>
        private static string DescribeMeleeVerbState(BdpVerb_MeleeAttackDamage verb)
        {
            if (verb == null)
            {
                return "null";
            }

            return "attackId=" + SafeText(verb.AttackInstanceId)
                + ", resultId=" + SafeText(verb.ResultId)
                + ", hostToken=" + DescribeToken(verb.HostSessionToken)
                + ", planToken=" + DescribeToken(verb.PlanSessionToken)
                + ", nextStepIndex=" + verb.NextRuntimeStepIndex
                + ", dispatchIntent=" + verb.PlanDispatchIntent
                + ", planReason=" + verb.PlanReason;
        }

        /// <summary>
        /// 输出模块会话摘要。
        /// </summary>
        private static string DescribeModuleSession(RangedAttackModuleSession session)
        {
            if (session == null)
            {
                return "null";
            }

            return "resultId=" + SafeText(session.Result != null ? session.Result.Id : null)
                + ", mounts=" + CountItems(session.Mounts)
                + ", slots=" + CountItems(session.Slots)
                + ", context={" + DescribeAttackContext(session.AttackContext) + "}";
        }

        /// <summary>
        /// 输出运行态攻击上下文摘要。
        /// </summary>
        private static string DescribeAttackContext(AttackContext attackContext)
        {
            return attackContext != null
                ? DescribeAttackContextSnapshot(attackContext.ToSnapshot())
                : "null";
        }

        /// <summary>
        /// 输出冻结攻击上下文摘要。
        /// </summary>
        private static string DescribeAttackContextSnapshot(AttackContextSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "null";
            }

            int entryCount = 0;
            int privateCount = 0;
            foreach (AttackContextSnapshot.Entry entry in snapshot.GetEntries())
            {
                if (entry?.Node == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                entryCount++;
                if (entry.Key.StartsWith(AttackContextKeys.ModulePrivatePrefix))
                {
                    privateCount++;
                }
            }

            return "entries=" + entryCount
                + "/private=" + privateCount
                + ", confirmedInput=" + (snapshot.GetNode(AttackContextKeys.ConfirmedInput) != null)
                + ", confirmedInteraction=" + (snapshot.GetNode(AttackContextKeys.ConfirmedInteraction) != null)
                + ", targetingInput=" + (snapshot.GetNode(AttackContextKeys.TargetingInputState) != null)
                + ", targetingInteraction=" + (snapshot.GetNode(AttackContextKeys.TargetingInteraction) != null);
        }

        /// <summary>
        /// 统计发射窗口数量。
        /// </summary>
        private static int CountWindows(RangedVerbEmissionPlan emissionPlan)
        {
            return emissionPlan != null && emissionPlan.Windows != null
                ? emissionPlan.Windows.Count
                : 0;
        }

        /// <summary>
        /// 统计发射计划期望输出的 emit 数量。
        /// </summary>
        private static int CountExpectedEmits(RangedVerbEmissionPlan emissionPlan)
        {
            if (emissionPlan == null)
            {
                return 0;
            }

            if (emissionPlan.ExpectedEmitCount > 0)
            {
                return emissionPlan.ExpectedEmitCount;
            }

            if (emissionPlan.Windows == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < emissionPlan.Windows.Count; i++)
            {
                RangedVerbEmissionWindowPlan window = emissionPlan.Windows[i];
                if (window == null)
                {
                    continue;
                }

                count += window.ExpectedEmitCount > 0
                    ? window.ExpectedEmitCount
                    : window.ProjectilePlans != null ? window.ProjectilePlans.Count : 0;
            }

            return count;
        }

        /// <summary>
        /// 输出“需要几次”的摘要。
        /// </summary>
        private static string DescribeRequiredCount(int requiredCount)
        {
            return requiredCount == int.MaxValue
                ? "continuous"
                : requiredCount.ToString();
        }

        /// <summary>
        /// 统计可空列表数量。
        /// </summary>
        private static int CountItems<T>(IReadOnlyList<T> items)
        {
            return items != null ? items.Count : 0;
        }

        /// <summary>
        /// 输出世界坐标向量摘要。
        /// 统一使用固定小数格式，便于日志人工对比与文本检索。
        /// </summary>
        private static string DescribeVector(Vector3 value)
        {
            return "(" + DescribeFloat(value.x) + "," + DescribeFloat(value.y) + "," + DescribeFloat(value.z) + ")";
        }

        /// <summary>
        /// 输出浮点数摘要。
        /// 使用不受系统区域影响的格式，避免日志中的小数点随系统语言变化。
        /// </summary>
        private static string DescribeFloat(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 统一清理空字符串。
        /// </summary>
        private static string SafeText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "<none>" : text;
        }
    }
}
