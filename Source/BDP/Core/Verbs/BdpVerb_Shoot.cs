using System.Collections.Generic;
using BDP.Core.CombatLog;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol;
using BDP.Core.AttackExecution.RangedProtocol.Aim;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Expressions;
using BDP.Core.Permissions;
using BDP.Core.Projectiles;
using BDP.Core.Projectiles.Interaction;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Semantics;
using BDP.Core.Trigger.Visual;
using BDP.Core.Trigger.Visual.Diagnostics;
using BDP.Core.VerbHosting;
using BDP.Support.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BDP.Core.Verbs
{
    /// <summary>
    /// BDP 远程 Verb 宿主。
    /// 它只负责把远程协议已经裁定好的宿主发射计划落成原版发射动作。
    /// </summary>
    public class BdpVerb_Shoot : Verb_Shoot, IBdpSemanticCarrier, IAttackEffectTraceCarrier
    {
        /// <summary>
        /// 当前宿主在 VerbHosting 层绑定的稳定正式结果标识。
        /// 它服务持续攻击会话重新准备下一轮协议输入。
        /// </summary>
        private AttackSessionToken hostSessionToken;

        /// <summary>
        /// 当前远程攻击实例标识。
        /// 它服务日志、效果追踪和同一次攻击会话的链路串联。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前这次远程发射实际落地的正式结果标识。
        /// 它用于语义追踪和投射物侧回溯，不承担重新解析职责。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前宿主在 VerbHosting 层绑定的稳定正式结果标识。
        /// 它服务持续攻击会话重新准备下一轮协议输入。
        /// </summary>
        public string HostResultId
        {
            get { return hostSessionToken != null ? hostSessionToken.ResultId : null; }
        }

        /// <summary>
        /// 当前宿主持有的正式攻击会话令牌。
        /// 持续攻击续射、读档续接和版本失效校验都只认它。
        /// </summary>
        internal AttackSessionToken HostSessionToken
        {
            get { return hostSessionToken; }
            set { hostSessionToken = value; }
        }

        /// <summary>
        /// 当前宿主持有的远程模块会话冻结态。
        /// 正常执行链应直接复用它，不再重新解释模块挂载。
        /// </summary>
        internal RangedAttackModuleSession HostModuleSession
        {
            get { return hostModuleSession; }
            set
            {
                hostModuleSession = value;
            }
        }

        /// <summary>
        /// 当前宿主冻结的统一攻击上下文快照。
        /// dual 多侧合并时不一定存在单一模块会话，续发重建要靠它恢复玩家已确认的路径状态。
        /// </summary>
        internal AttackContextSnapshot HostAttackContextSnapshot
        {
            get { return hostAttackContextSnapshot; }
        }

        /// <summary>
        /// 判断实际起手目标是否仍与当前自动会话一致；实体和格子目标都必须精确匹配。
        /// </summary>
        internal bool MatchesAutomaticCastTarget(LocalTargetInfo target)
        {
            if (!TryResolveCurrentSemanticTarget(out LocalTargetInfo currentTarget)
                && sessionTarget.IsValid)
            {
                currentTarget = sessionTarget;
            }

            return target.IsValid && currentTarget == target;
        }

        /// <summary>实际可命中的起手才允许准备会话；普通远程基类沿用已经准备好的上下文。</summary>
        protected virtual bool TryPrepareCastSession(LocalTargetInfo target) => true;

        /// <summary>
        /// 当前这次远程发射携带的战斗语义。
        /// 它只服务效果和伤害链，不回写表达层。
        /// </summary>
        /// <summary>
        /// 在入口框架侧暂存一份待起手的模块会话。
        /// 它只服务新一轮起手准备，不得覆盖正式执行期间的提交会话。
        /// </summary>
        internal void StageEntryModuleSession(RangedAttackModuleSession session)
        {
            stagedEntryModuleSession = session;
        }

        /// <summary>
        /// 读取当前起手准备应使用的模块会话。
        /// 优先复用正式执行会话，只有其不存在时才回落到入口暂存会话。
        /// </summary>
        internal RangedAttackModuleSession ResolveEntryModuleSession()
        {
            if (HostModuleSession != null && stagedEntryModuleSession != null)
            {
                AttackExecutionDiagnostics.LogEntryModuleSessionResolution(
                    CasterPawn,
                    this,
                    HostSessionToken,
                    HostModuleSession,
                    stagedEntryModuleSession,
                    "resident_host",
                    "resident_over_staged_conflict");
            }

            return HostModuleSession ?? stagedEntryModuleSession;
        }

        /// <summary>
        /// 读取当前攻击会话冻结的语义目标。
        /// 持续攻击的停火判断用它识别毒蛇这类路径攻击背后的真实实体目标。
        /// </summary>
        internal bool TryResolveCurrentSemanticTarget(out LocalTargetInfo target)
        {
            if (TryResolveSemanticTargetFromAttackContext(HostModuleSession?.AttackContext, out target)
                || TryResolveSemanticTargetFromAttackContext(stagedEntryModuleSession?.AttackContext, out target))
            {
                return true;
            }

            ResolvePreparedPlanDiagnosticTargets(
                out _,
                out LocalTargetInfo preparedAimTarget,
                out LocalTargetInfo preparedCurrentTarget);
            if (preparedCurrentTarget.IsValid)
            {
                target = preparedCurrentTarget;
                return true;
            }

            if (preparedAimTarget.IsValid)
            {
                target = preparedAimTarget;
                return true;
            }

            target = LocalTargetInfo.Invalid;
            return false;
        }

        /// <summary>
        /// 从统一攻击上下文读取确认阶段冻结的语义目标。
        /// </summary>
        private static bool TryResolveSemanticTargetFromAttackContext(AttackContext attackContext, out LocalTargetInfo target)
        {
            target = LocalTargetInfo.Invalid;
            ConfirmedTargetSnapshot confirmedTarget = attackContext?.Get<ConfirmedTargetSnapshot>(AttackContextKeys.ConfirmedTarget);
            if (confirmedTarget == null || !confirmedTarget.SemanticTarget.IsValid)
            {
                return false;
            }

            target = confirmedTarget.SemanticTarget;
            return true;
        }

        /// <summary>
        /// 清空入口框架暂存的模块会话。
        /// 正式执行一旦提交或重置，旧的入口暂存会话就不应再参与后续流程。
        /// </summary>
        internal void ClearStagedEntryModuleSession()
        {
            if (stagedEntryModuleSession != null)
            {
                AttackExecutionDiagnostics.LogEntryModuleSessionCleared(
                    CasterPawn,
                    this,
                    HostSessionToken,
                    stagedEntryModuleSession,
                    "clear_staged_entry");
            }

            stagedEntryModuleSession = null;
        }

        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前这一轮宿主会话绑定的正式运行时 Verb 规格。
        /// 它是本轮真正应消费的发射真值来源。
        /// </summary>
        private ResolvedVerbSpec currentResolvedVerbSpec;

        /// <summary>
        /// 当前持续攻击宿主持有的模块会话冻结态。
        /// 它服务续射重备，不进入正式存档契约。
        /// </summary>
        private RangedAttackModuleSession hostModuleSession;

        /// <summary>
        /// 当前持续攻击宿主保留的攻击上下文快照。
        /// 它只保存中性上下文节点，不保存模块运行时对象。
        /// </summary>
        private AttackContextSnapshot hostAttackContextSnapshot;

        /// <summary>
        /// 当前入口框架暂存的模块会话。
        /// 它只服务新一轮起手准备，不得覆盖已提交的正式执行会话。
        /// </summary>
        private RangedAttackModuleSession stagedEntryModuleSession;

        /// <summary>
        /// 当前攻击会话确认的稳定目标。
        /// 它服务续射重建，不随单次发射的首段目标被覆盖。
        /// </summary>
        private LocalTargetInfo sessionTarget = LocalTargetInfo.Invalid;

        /// <summary>
        /// 当前自动远程会话的目标是否经历过一次倒地、死亡或恢复行动。
        /// 标记只保留到安全的宿主收口点，不进入正式存档契约。
        /// </summary>
        private bool targetLifecycleChanged;

        /// <summary>
        /// 当前下一发投射物的发射原点偏移。
        /// 它只在宿主实际生成投射物时消费，不参与协议真值裁决。
        /// </summary>
        private Vector3 nextOriginOffset;

        /// <summary>
        /// 当前远程宿主单轮发射状态。
        /// </summary>
        private readonly RangedVerbRoundState roundState = new RangedVerbRoundState();

        /// <summary>
        /// 当前远程宿主发射游标。
        /// </summary>
        private readonly RangedVerbEmissionCursor emissionCursor = new RangedVerbEmissionCursor();

        /// <summary>
        /// 当前远程宿主续射规划器。
        /// </summary>
        private readonly RangedVerbContinuationPlanner continuationPlanner = new RangedVerbContinuationPlanner();

        /// <summary>
        /// 当前已对外提示过 Trion 不足的攻击会话标识。
        /// 同一条持续攻击会话内只弹一次；会话切换、打断或恢复可负担后重置。
        /// </summary>
        private string insufficientTrionPromptLatchedAttackInstanceId;

        /// <summary>
        /// 当前发射计划的首发横向侧别是否已经随机确定。
        /// </summary>
        private bool originSpreadSideInitialized;

        /// <summary>
        /// 已经确定首发横向侧别的正式发射计划引用。
        /// </summary>
        private RangedVerbEmissionPlan originSpreadSidePlan;

        /// <summary>
        /// 当前正式发射计划的首发横向侧别。
        /// true 表示首发向右，false 表示首发向左。
        /// </summary>
        private bool originSpreadStartsRight;

        /// <summary>
        /// 对内暴露当前远程宿主单轮发射状态。
        /// </summary>
        internal RangedVerbRoundState RoundState
        {
            get { return roundState; }
        }

        /// <summary>
        /// 对内暴露当前远程宿主发射游标。
        /// </summary>
        internal RangedVerbEmissionCursor EmissionCursor
        {
            get { return emissionCursor; }
        }

        /// <summary>
        /// 序列化 formal host 宿主跨档续接所需的最小会话真值。
        /// 派生 plan 与窗口对象继续走读档后惰性重建，不进入正式存档契约。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref hostSessionToken, "hostSessionToken");
            Scribe_Deep.Look(ref hostAttackContextSnapshot, "hostAttackContextSnapshot");
            Scribe_TargetInfo.Look(ref sessionTarget, "sessionTarget");
            emissionCursor.ExposeData();
            roundState.ExposeData();
        }

        /// <summary>
        /// 把远程执行上下文绑定到当前宿主。
        /// 从这一刻起，续射重备也必须沿用同一份投影版本号。
        /// </summary>
        internal void ApplyExecutionContext(RangedAttackExecutionContext context)
        {
            AttackSessionToken previousSessionToken = HostSessionToken != null ? HostSessionToken.Clone() : null;
            if (context == null)
            {
                AttackExecutionVisualRuntimeBridge.Clear(CasterPawn, previousSessionToken);
                LogSessionClearedIfNeeded("apply_context_null");
                AttackInstanceId = null;
                ResultId = null;
                HostSessionToken = null;
                HostModuleSession = null;
                hostAttackContextSnapshot = null;
                ClearStagedEntryModuleSession();
                sessionTarget = LocalTargetInfo.Invalid;
                targetLifecycleChanged = false;
                SemanticContext = null;
                currentResolvedVerbSpec = null;
                ResetInsufficientTrionPromptLatch();
                ResetOriginSpreadSideState();
                roundState.Reset();
                return;
            }

            AttackInstanceId = context.ProtocolResult?.Entry != null
                ? context.ProtocolResult.Entry.AttackInstanceId
                : context.Step != null ? context.Step.AttackInstanceId : null;
            ResultId = context.ProtocolResult?.Entry != null
                ? context.ProtocolResult.Entry.SourceResultId
                : context.SessionResult != null ? context.SessionResult.Id : null;
            HostSessionToken = AttackSessionToken.Create(
                context.Pawn ?? CasterPawn,
                context.HostResultId,
                context.ProjectionVersion,
                AttackInstanceId);
            HostModuleSession = context.ProtocolResult?.Entry != null
                ? context.ProtocolResult.Entry.ModuleSession
                : null;
            hostAttackContextSnapshot = ResolveProtocolAttackContextSnapshot(context);
            ClearStagedEntryModuleSession();
            sessionTarget = context.SessionTarget;
            SemanticContext = context.ProtocolResult?.Entry != null
                ? context.ProtocolResult.Entry.SemanticContext
                : context.SessionResult != null ? context.SessionResult.SemanticContext : null;
            currentResolvedVerbSpec = context.ResolvedVerbSpec;
            AttackExecutionVisualRuntimeBridge.Publish(context);
            SyncInsufficientTrionPromptLatchToCurrentSession();
            roundState.ApplyExecutionContext(context);
            AttackExecutionDiagnostics.LogHostSessionBound(
                context.Pawn ?? CasterPawn,
                this,
                HostSessionToken,
                context.HostResultId,
                sessionTarget,
                HostModuleSession);
        }

        /// <summary>
        /// 从远程协议结果中冻结一份续发可复用的攻击上下文。
        /// 优先使用协议入口的合并上下文；没有时再退回模块会话上下文或首个投射计划快照。
        /// </summary>
        /// <param name="context">当前远程执行上下文。</param>
        /// <returns>可用于续发重建的攻击上下文快照。</returns>
        private static AttackContextSnapshot ResolveProtocolAttackContextSnapshot(RangedAttackExecutionContext context)
        {
            if (context?.ProtocolResult?.Entry?.AttackContext != null)
            {
                return context.ProtocolResult.Entry.AttackContext.ToSnapshot();
            }

            if (context?.ProtocolResult?.Entry?.ModuleSession?.AttackContext != null)
            {
                return context.ProtocolResult.Entry.ModuleSession.AttackContext.ToSnapshot();
            }

            IReadOnlyList<ProjectileInitPlan> projectilePlans = context?.ProtocolResult?.ProjectilePlans;
            if (projectilePlans != null)
            {
                for (int i = 0; i < projectilePlans.Count; i++)
                {
                    if (projectilePlans[i]?.AttackContextSnapshot != null)
                    {
                        return projectilePlans[i].AttackContextSnapshot;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 绑定当前动作步正式裁定好的宿主发射计划。
        /// 发射桥只消费它的副本，不回写协议真值对象。
        /// </summary>
        internal void BindVerbEmissionPlan(RangedVerbEmissionPlan emissionPlan)
        {
            emissionCursor.BindVerbEmissionPlan(emissionPlan);
            AttackExecutionVisualRuntimeBridge.PublishAttackParticipants(
                CasterPawn,
                HostSessionToken,
                emissionPlan);
            if (emissionPlan == null)
            {
                roundState.Reset();
            }
        }

        /// <summary>自有远程入口原生执行动态准入，再保留原版可用性。</summary>
        public override bool Available()
        {
            return AttackAdmissionAccess.Evaluate(CasterPawn, this).Accepted && base.Available();
        }

        /// <summary>准备发射计划和资源承诺前先检查攻击准入。</summary>
        public override bool TryStartCastOn(
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack = false,
            bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false,
            bool nonInterruptingSelfCast = false)
        {
            if (!AttackAdmissionAccess.Evaluate(CasterPawn, this).Accepted) return false;
            LocalTargetInfo target = castTarg.IsValid ? castTarg : destTarg;
            if (!target.IsValid)
            {
                return false;
            }

            ResolveExecutionRequestRouting(out AttackExecutionReason reason, out AttackDispatchIntent dispatchIntent);
            bool canHitTarget = CanHitTarget(target);
            bool canHitTargetFromCurrentPos = CanHitTargetFrom(caster.Position, target);
            bool hasShootLineToRequestedTarget = TryResolveShootLineDiagnostic(target);
            ResolvePreparedPlanDiagnosticTargets(
                out LocalTargetInfo preparedLaunchTarget,
                out LocalTargetInfo preparedAimTarget,
                out LocalTargetInfo preparedCurrentTarget);
            bool hasShootLineToPreparedLaunchTarget = TryResolveShootLineDiagnostic(preparedLaunchTarget);
            AttackExecutionDiagnostics.LogVerbCastAttempt(
                CasterPawn,
                this,
                HostSessionToken,
                reason,
                dispatchIntent,
                target,
                sessionTarget,
                currentTarget,
                preparedLaunchTarget,
                preparedAimTarget,
                preparedCurrentTarget,
                canHitTarget,
                canHitTargetFromCurrentPos,
                hasShootLineToRequestedTarget,
                hasShootLineToPreparedLaunchTarget,
                HasPendingEmissionPlan(),
                ResolveRemainingWindowCount(),
                ResolveRemainingProjectileCount());
            LogStalePendingEmissionPlanIfNeeded(target);
            // 原版 Verb.TryStartCastOn 会先拒绝当前不可命中的目标，
            // 不能在这个阶段先重建 plan 或触发资源准入。
            if (!canHitTarget)
            {
                AttackExecutionDiagnostics.LogVerbCastResult(
                    CasterPawn,
                    this,
                    HostSessionToken,
                    reason,
                    dispatchIntent,
                    "target_not_currently_hittable",
                    target,
                    preparedLaunchTarget,
                    preparedAimTarget,
                    preparedCurrentTarget,
                    false,
                    WarmingUp,
                    Bursting,
                    CasterPawn?.stances?.FullBodyBusy ?? false,
                    state.ToString(),
                    HasPendingEmissionPlan(),
                    ResolveRemainingWindowCount(),
                    ResolveRemainingProjectileCount());
                return false;
            }

            // 武器查询不得占有攻击会话。实际起手在可命中检查之后才处理换目标和首次准备。
            if (!TryPrepareCastSession(target)) return false;

            // 每次新的起手请求都必须丢弃旧 plan，避免 formal host 壳把上一轮目标泄漏到第一枪。
            ClearPendingEmissionPlan();
            if (!TryPreparePendingEmission(target))
            {
                AttackExecutionDiagnostics.LogVerbCastResult(
                    CasterPawn,
                    this,
                    HostSessionToken,
                    reason,
                    dispatchIntent,
                    "prepare_failed",
                    target,
                    LocalTargetInfo.Invalid,
                    LocalTargetInfo.Invalid,
                    LocalTargetInfo.Invalid,
                    false,
                    WarmingUp,
                    Bursting,
                    CasterPawn?.stances?.FullBodyBusy ?? false,
                    state.ToString(),
                    HasPendingEmissionPlan(),
                    ResolveRemainingWindowCount(),
                    ResolveRemainingProjectileCount());
                return false;
            }

            if (!TryEnsureRoundTrionAdmission())
            {
                ResolvePreparedPlanDiagnosticTargets(
                    out preparedLaunchTarget,
                    out preparedAimTarget,
                    out preparedCurrentTarget);
                ClearPendingEmissionPlan();
                AttackExecutionDiagnostics.LogVerbCastResult(
                    CasterPawn,
                    this,
                    HostSessionToken,
                    reason,
                    dispatchIntent,
                    "trion_reject",
                    target,
                    preparedLaunchTarget,
                    preparedAimTarget,
                    preparedCurrentTarget,
                    false,
                    WarmingUp,
                    Bursting,
                    CasterPawn?.stances?.FullBodyBusy ?? false,
                    state.ToString(),
                    HasPendingEmissionPlan(),
                    ResolveRemainingWindowCount(),
                    ResolveRemainingProjectileCount());
                return false;
            }

            ResolvePreparedPlanDiagnosticTargets(
                out preparedLaunchTarget,
                out preparedAimTarget,
                out preparedCurrentTarget);
            LogPreparedTargetMismatchIfNeeded(target);
            LocalTargetInfo baseCastTarget = ResolveBaseVerbStartTarget(target, preparedLaunchTarget);
            // 传给原版的首段目标同时决定 Stance_Warmup 和后续 Stance_Cooldown 的焦点。
            // 毒蛇起手成功后不能把 currentTarget 改回请求目标，否则首发后会转向最终目标。
            bool started = base.TryStartCastOn(baseCastTarget, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
            // 原版 Verb.TryStartCastOn 在 CanHitTarget 通过后，还会调用 TryFindShootLineFromTo 做暖机瞄准线检查。
            // 对毒蛇这类 DirectTargetLineOfSight=NotRequired 的武器，CanHitTarget 可通过 dual adapter 放行，
            // 但 TryFindShootLineFromTo 仍要求直接视线 → 对墙后目标返回 false → 整个攻击被拒绝。
            // 这里补一段 fallback：base 失败时，若我们的规则允许目标、仅瞄准线不通过，则手动构建暖机姿态。
            if (!started
                && baseCastTarget.IsValid
                && CanHitTargetFrom(caster.Position, baseCastTarget)
                && !TryFindShootLineFromTo(caster.Position, baseCastTarget, out _))
            {
                // 用一条指向目标的"虚拟瞄准线"代替原版需要的真实视线线
                ShootLine fallbackLine = new ShootLine(caster.Position, baseCastTarget.Cell);
                CasterPawn.Drawer.Notify_WarmingCastAlongLine(fallbackLine, caster.Position);
                float statValue = CasterPawn.GetStatValue(StatDefOf.AimingDelayFactor);
                int ticks = (WarmupTime * statValue).SecondsToTicks();
                CasterPawn.stances.SetStance(new Stance_Warmup(ticks, baseCastTarget, this));
                if (verbProps.stunTargetOnCastStart && baseCastTarget.Pawn != null)
                {
                    baseCastTarget.Pawn.stances.stunner.StunFor(ticks, null, addBattleLog: false);
                }
                started = true;
            }

            AttackExecutionDiagnostics.LogVerbCastResult(
                CasterPawn,
                this,
                HostSessionToken,
                reason,
                dispatchIntent,
                started ? "started" : "base_returned_false",
                target,
                preparedLaunchTarget,
                preparedAimTarget,
                preparedCurrentTarget,
                started,
                WarmingUp,
                Bursting,
                CasterPawn?.stances?.FullBodyBusy ?? false,
                state.ToString(),
                HasPendingEmissionPlan(),
                ResolveRemainingWindowCount(),
                ResolveRemainingProjectileCount());
            return started;
        }

        public override void WarmupComplete()
        {
            LocalTargetInfo warmupTarget = sessionTarget.IsValid ? sessionTarget : currentTarget;
            bool canRefreshPendingEmission = warmupTarget.IsValid
                && HostSessionToken != null
                && !string.IsNullOrWhiteSpace(HostSessionToken.ResultId)
                && HostSessionToken.ProjectionVersion > 0;
            bool rebuildSucceeded;
            if (canRefreshPendingEmission)
            {
                ClearPendingEmissionPlan();
                rebuildSucceeded = TryPreparePendingEmission(warmupTarget);
            }
            else
            {
                rebuildSucceeded = HasPendingEmissionPlan() || TryPreparePendingEmission(warmupTarget);
            }
            if (!rebuildSucceeded)
            {
                // 暖机完成时续射准备失败，当前正式宿主已经无法继续承担本轮攻击。
                // 必须完整关闭会话，否则原版姿态结束后会再次尝试同一份失效上下文。
                Reset();
                return;
            }

            LogWarmupBattleEntry();
            burstShotsLeft = ResolveRemainingWindowCount();
            state = VerbState.Bursting;
            TriggerVisualEmissionDiagnosticsAccess.BeginBurstBatch(
                CasterPawn,
                HostSessionToken != null && !string.IsNullOrWhiteSpace(HostSessionToken.AttackInstanceId)
                    ? HostSessionToken.AttackInstanceId
                    : AttackInstanceId);
            TryCastNextBurstShot();
            GrantShootingExperience();
        }

        /// <summary>
        /// 记录一次已经通过发射前校验的真实开火时刻。
        /// 它只更新原版 Verb 后坐力读取的 lastShotTick，不创建额外后坐力状态。
        /// </summary>
        internal void NotifyShotFiredForRecoil(int shotTick)
        {
            lastShotTick = shotTick;
        }

        /// <summary>
        /// 按发射计划已经裁定的来源结果标识，同步该来源正式远程 Verb 的原版开火时刻。
        /// 来源绑定缺失时安全跳过，当前执行宿主仍保留自己的原版状态。
        /// </summary>
        private void NotifySourceShotFiredForRecoil(string sourceResultId, int shotTick)
        {
            if (!VerbHostSurfaceAccess.TryGetByResultId(
                    CasterPawn,
                    sourceResultId,
                    out BdpFormalVerbBinding binding)
                || binding?.RangedVerb == null)
            {
                return;
            }

            binding.RangedVerb.NotifyShotFiredForRecoil(shotTick);
        }

        /// <summary>
        /// 直接派发一次 projectile 初始化计划。
        /// 这里不再重算复杂远程意图，只把计划落成原版 projectile spawn。
        /// </summary>
        internal bool TryEmitPlan(ProjectileInitPlan plan)
        {
            if (plan == null)
            {
                return false;
            }

            LocalTargetInfo semanticTarget = plan.CurrentTarget.IsValid
                ? plan.CurrentTarget
                : plan.AimTarget.IsValid
                ? plan.AimTarget
                : currentTarget;
            LocalTargetInfo navigationTarget = plan.LaunchTarget.IsValid
                ? plan.LaunchTarget
                : semanticTarget;
            if (!navigationTarget.IsValid)
            {
                return false;
            }

            ThingDef projectileDef = plan.ProjectileDef
                ?? (currentResolvedVerbSpec != null ? currentResolvedVerbSpec.ProjectileDef : null)
                ?? Projectile;
            if (projectileDef == null)
            {
                return false;
            }

            // 原版 VerbProperties.stopBurstWithoutLos 默认开启；高角投射物本身就是绕过地面直视线的能力边界。
            // 先解析本发计划，才能在无直视线时识别 flyOverhead 并继续进入原版 Launch（发射）。
            bool allowsMissingShootLine = projectileDef.projectile != null
                && projectileDef.projectile.flyOverhead;
            AttackInstanceId = plan.AttackInstanceId;
            ResultId = plan.ResultId;
            SemanticContext = plan.SemanticContext;

            if (navigationTarget.HasThing && navigationTarget.Thing.Map != caster.Map)
            {
                return false;
            }

            ShootLine resultingLine;
            bool hasLine = TryFindShootLineFromTo(caster.Position, navigationTarget, out resultingLine);
            bool hasIndirectAttackPermission = !hasLine
                && !allowsMissingShootLine
                && TryResolveIndirectAttackPermission(
                    plan,
                    semanticTarget,
                    out _);
            if (!hasLine && hasIndirectAttackPermission)
            {
                // 原版 TryFindShootLineFromTo 只提供直射线；间接许可通过后，
                // 仍需给后续 ShotReport、Launch 和路线协议一条稳定的几何射击线。
                resultingLine = new ShootLine(caster.Position, navigationTarget.Cell);
            }

            bool canLaunchWithoutShootLine = allowsMissingShootLine || hasIndirectAttackPermission;
            if (verbProps.stopBurstWithoutLos && !hasLine && !canLaunchWithoutShootLine)
            {
                return false;
            }

            Thing manningPawn = caster;
            Thing equipmentSource = EquipmentSource;
            CompMannable compMannable = caster.TryGetComp<CompMannable>();
            if (compMannable?.ManningPawn != null)
            {
                manningPawn = compMannable.ManningPawn;
                equipmentSource = caster;
            }

            Vector3 drawPos = caster.DrawPos;
            TriggerVisualLaunchOriginResolution rootResolution =
                TriggerVisualLaunchOriginResolver.ResolveLaunchRoot(
                    CasterPawn,
                    plan.ResultId,
                    plan.HasAbsoluteOriginWorld,
                    plan.AbsoluteOriginWorld,
                    drawPos);
            Vector3 rootOrigin = rootResolution != null && rootResolution.HasRootOrigin
                ? rootResolution.RootOriginWorld
                : drawPos;
            Vector3 theoreticalOrigin = rootOrigin + plan.OriginOffsetWorld;
            int expectedEmitCount = ResolveExpectedOriginSpreadEmitCount();
            Vector3 launchOrigin = theoreticalOrigin
                + ResolveRandomOriginSpreadOffset(theoreticalOrigin, navigationTarget, plan, expectedEmitCount);
            TriggerVisualEmissionDiagnosticsAccess.RecordLaunchOrigin(
                CasterPawn,
                plan.AttackInstanceId,
                plan.ResultId,
                rootOrigin,
                theoreticalOrigin,
                launchOrigin,
                plan.OriginOffsetWorld,
                rootResolution != null
                    ? rootResolution.SourceKind.ToString()
                    : TriggerVisualLaunchOriginSourceKind.CasterDrawPosFallback.ToString(),
                rootResolution != null
                    ? rootResolution.VisualFailureKind.ToString()
                    : TriggerVisualLaunchOriginSourceKind.MissingTriggerBody.ToString(),
                plan.HasAbsoluteOriginWorld,
                rootResolution != null ? rootResolution.ProjectionVersion : 0,
                rootResolution != null ? rootResolution.PoseSampleTick : 0);
            nextOriginOffset = launchOrigin - drawPos;
            bool emitted = TryLaunchSinglePlan(projectileDef, resultingLine, manningPawn, equipmentSource, launchOrigin, plan, semanticTarget, navigationTarget);
            nextOriginOffset = Vector3.zero;
            if (emitted)
            {
                // 只有原版 Launch（发射）成功后才消费弹药、充能和后坐力时刻。
                // 射击报告或发射前校验失败时，不应留下“已消费但没有子弹”的半次攻击。
                EquipmentSource?.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
                EquipmentSource?.GetComp<CompApparelVerbOwner_Charged>()?.UsedOnce();
                int shotTick = Find.TickManager.TicksGame;
                NotifyShotFiredForRecoil(shotTick);
                NotifySourceShotFiredForRecoil(plan.ResultId, shotTick);
                // 临时诊断：记录真实 projectile 落地 Tick，并同时记录原版声明值与解析值。
                // 这里不改变攻击状态，只为确认双持变慢发生在“每发间隔”还是“整轮窗口数量”。
                AttackExecutionDiagnostics.LogRangedShotTiming(
                    CasterPawn,
                    this,
                    shotTick,
                    emissionCursor.PendingWindowIndex,
                    emissionCursor.PendingEmissionWindows != null
                        ? emissionCursor.PendingEmissionWindows.Count
                        : 0,
                    verbProps != null ? verbProps.ticksBetweenBurstShots : -1,
                    TicksBetweenBurstShots,
                    plan.AttackInstanceId,
                    HostResultId,
                    plan.ResultId,
                    burstShotsLeft,
                    emissionCursor.ResolveRemainingWindowCount(),
                    CasterPawn?.stances?.FullBodyBusy ?? false,
                    state.ToString());
            }
            if (emitted && plan.MuzzleFlashScale > 0.01f)
            {
                // 继续调用原版射击闪光接口，但位置归属当前实际发射来源的枪口。
                FleckMaker.Static(
                    rootOrigin,
                    caster.Map,
                    FleckDefOf.ShotFlash,
                    plan.MuzzleFlashScale);
            }
            return emitted;
        }

        /// <summary>
        /// 查询当前发射计划是否获得了可绕过原版直射线发射前置的间接攻击许可。
        /// 目标合法性阶段已经做过一次查询；这里在真正发射前再次按当前计划和目标读取，
        /// 避免只放宽瞄准界面而仍被原版 stopBurstWithoutLos（无直视停止连发）拦截。
        /// </summary>
        private bool TryResolveIndirectAttackPermission(
            ProjectileInitPlan plan,
            LocalTargetInfo target,
            out IndirectRangedAttackPermissionGrant grant)
        {
            grant = null;
            string resultId = plan != null && !string.IsNullOrWhiteSpace(plan.ResultId)
                ? plan.ResultId
                : HostResultId;
            if (CasterPawn == null || string.IsNullOrWhiteSpace(resultId))
            {
                return false;
            }

            if (!AttackExecutionSurfaceAccess.TryGetPublishedResult(
                    CasterPawn,
                    resultId,
                    out _,
                    out FormalExpressionResult result))
            {
                return false;
            }

            return IndirectRangedAttackPermissionAccess.TryResolve(
                CasterPawn,
                result,
                target,
                out grant);
        }

        protected override bool TryCastShot()
        {
            if (!HasPendingEmissionPlan() && !TryPreparePendingEmission(sessionTarget.IsValid ? sessionTarget : currentTarget))
            {
                Reset();
                return false;
            }

            if (!TryGetCurrentWindow(out RangedVerbEmissionWindowPlan window))
            {
                Reset();
                return false;
            }

            if (!TryValidateCurrentEmissionWindowTargets())
            {
                return false;
            }

            if (!TryCommitRoundTrionBeforeFirstEmission())
            {
                return false;
            }

            int emittedCount = 0;
            if (window.EmissionMode == RangedVerbEmissionMode.SimultaneousStep)
            {
                while (TryBindNextWindowPlan(out ProjectileInitPlan plan))
                {
                    if (!TryEmitPlan(plan))
                    {
                        continue;
                    }

                    emittedCount++;
                }
            }
            else
            {
                if (!TryBindNextWindowPlan(out ProjectileInitPlan plan))
                {
                    return false;
                }

                if (TryEmitPlan(plan))
                {
                    emittedCount = 1;
                }
            }

            emissionCursor.AdvanceAfterCurrentWindow(emittedCount);

            if (!HasPendingEmissionPlan())
            {
                LogCurrentVerbEmissionSummary();
                ClearPendingEmissionPlan();
            }

            return emittedCount > 0;
        }

        /// <summary>
        /// 发射单个 projectile 初始化计划。
        /// 这里只处理如何自然接入原版 projectile 行为，不解释协议业务含义。
        /// </summary>
        private bool TryLaunchSinglePlan(
            ThingDef projectileDef,
            ShootLine resultingLine,
            Thing manningPawn,
            Thing equipmentSource,
            Vector3 drawPos,
            ProjectileInitPlan plan,
            LocalTargetInfo semanticTarget,
            LocalTargetInfo navigationTarget)
        {
            float accuracyFactor = ResolveAccuracyFactor(plan);
            float forcedMissRadius = ResolveForcedMissRadius(plan);
            if (forcedMissRadius > 0.5f)
            {
                if (manningPawn is Pawn pawn)
                {
                    forcedMissRadius *= verbProps.GetForceMissFactorFor(equipmentSource, pawn);
                }

                float adjustedForcedMiss = VerbUtility.CalculateAdjustedForcedMiss(forcedMissRadius, navigationTarget.Cell - caster.Position);
                if (adjustedForcedMiss > 0.5f)
                {
                    IntVec3 forcedMissTarget = GetForcedMissTarget(adjustedForcedMiss);
                    if (forcedMissTarget != navigationTarget.Cell)
                    {
                        Projectile projectileThing = CreateProjectileForLaunch(
                            projectileDef,
                            resultingLine.Source,
                            equipmentSource,
                            plan);
                        if (projectileThing == null)
                        {
                            return false;
                        }

                        ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                        if (Rand.Chance(0.5f))
                        {
                            projectileHitFlags = ProjectileHitFlags.All;
                        }

                        if (!canHitNonTargetPawnsNow)
                        {
                            projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                        }

                        LogProjectileLaunchDecision(
                            "forced_miss",
                            projectileThing,
                            plan,
                            drawPos,
                            resultingLine,
                            semanticTarget,
                            navigationTarget,
                            forcedMissTarget,
                            semanticTarget,
                            projectileHitFlags,
                            preventFriendlyFire,
                            canHitNonTargetPawnsNow,
                            null,
                            forcedMissRadius,
                            accuracyFactor);
                        FreezeLaunchResolution(
                            plan,
                            ProjectileLaunchDisposition.ForcedMiss,
                            false,
                            ProjectileLaunchContinuationMode.None,
                            new LocalTargetInfo(forcedMissTarget),
                            forcedMissTarget.ToVector3Shifted(),
                            Vector3.zero,
                            null);
                        projectileThing.Launch(manningPawn, drawPos, forcedMissTarget, semanticTarget, projectileHitFlags, preventFriendlyFire, equipmentSource);
                        return true;
                    }
                }
            }

            // 若当前 projectile 携带来源芯片的独立精度，临时注入 verbProps 供 ShotReport 消费。
            // 双持场景下不同 projectile 可能来自不同芯片（精度不同），而 ShotReport 只能从
            // Verb.verbProps 读精度——这里把 verbProps 暂换成 plan 来源芯片的精度再恢复。
            float savedAccuracyTouch = 0f, savedAccuracyShort = 0f, savedAccuracyMedium = 0f, savedAccuracyLong = 0f;
            bool hasPlanAccuracy = plan != null && plan.HasAccuracy;
            if (hasPlanAccuracy)
            {
                savedAccuracyTouch = verbProps.accuracyTouch;
                savedAccuracyShort = verbProps.accuracyShort;
                savedAccuracyMedium = verbProps.accuracyMedium;
                savedAccuracyLong = verbProps.accuracyLong;
                verbProps.accuracyTouch = plan.AccuracyTouch;
                verbProps.accuracyShort = plan.AccuracyShort;
                verbProps.accuracyMedium = plan.AccuracyMedium;
                verbProps.accuracyLong = plan.AccuracyLong;
            }

            ShotReport shotReport;
            try
            {
                shotReport = ShotReport.HitReportFor(caster, this, semanticTarget);
            }
            finally
            {
                if (hasPlanAccuracy)
                {
                    verbProps.accuracyTouch = savedAccuracyTouch;
                    verbProps.accuracyShort = savedAccuracyShort;
                    verbProps.accuracyMedium = savedAccuracyMedium;
                    verbProps.accuracyLong = savedAccuracyLong;
                }
            }

            // 必须消费该发真实报告：来源芯片、距离、天气与既有倍率在此已明确。
            // 模块只改变瞄准概率，仍由下面的原版分支完成唯一一次随机判定。
            accuracyFactor = ResolveReportAccuracyFactor(plan, shotReport, accuracyFactor);
            CaptureAccuracySnapshot(plan, shotReport, accuracyFactor, forcedMissRadius);
            float adjustedAimOnTargetIgnoringPosture = Mathf.Clamp01(shotReport.AimOnTargetChance_IgnoringPosture * accuracyFactor);
            float adjustedAimOnTargetStandardTarget = Mathf.Clamp01(shotReport.AimOnTargetChance_StandardTarget * accuracyFactor);
            IntVec3 coverOriginCell = ResolveCoverOriginCell(plan, resultingLine, semanticTarget);
            TrajectoryCoverSnapshot trajectoryCover = TrajectoryCoverSnapshot.Create(
                semanticTarget,
                coverOriginCell,
                caster.Map);
            plan.HasFinalApproachStartCell = trajectoryCover.HasCoverOriginCell;
            plan.FinalApproachStartCell = trajectoryCover.CoverOriginCell;
            plan.TrajectoryCover = trajectoryCover;
            Thing randomCoverToMissInto = trajectoryCover.RandomCover;
            ThingDef targetCoverDef = randomCoverToMissInto?.def;
            if (plan.AccuracySnapshot != null)
            {
                plan.AccuracySnapshot.PassCoverChance = Mathf.Clamp01(1f - trajectoryCover.OverallBlockChance);
            }
            if (verbProps.canGoWild && !Rand.Chance(adjustedAimOnTargetIgnoringPosture))
            {
                bool flyOverhead = projectileDef.projectile != null && projectileDef.projectile.flyOverhead;
                Vector3 nominalWildDestination = resultingLine.Dest.ToVector3Shifted();
                resultingLine.ChangeDestToMissWild(adjustedAimOnTargetStandardTarget, flyOverhead, caster.Map);
                Vector3 actualWildDestination = resultingLine.Dest.ToVector3Shifted();
                Projectile projectileThing = CreateProjectileForLaunch(
                    projectileDef,
                    resultingLine.Source,
                    equipmentSource,
                    plan);
                if (projectileThing == null)
                {
                    return false;
                }

                ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
                {
                    projectileHitFlags |= ProjectileHitFlags.NonTargetPawns;
                }

                LogProjectileLaunchDecision(
                    "wild_miss",
                    projectileThing,
                    plan,
                    drawPos,
                    resultingLine,
                    semanticTarget,
                    navigationTarget,
                    resultingLine.Dest,
                    semanticTarget,
                    projectileHitFlags,
                    preventFriendlyFire,
                    canHitNonTargetPawnsNow,
                    targetCoverDef,
                    forcedMissRadius,
                    accuracyFactor);
                FreezeLaunchResolution(
                    plan,
                    ProjectileLaunchDisposition.WildMiss,
                    false,
                    ProjectileLaunchContinuationMode.WildMissOffsetRoute,
                    new LocalTargetInfo(resultingLine.Dest),
                    actualWildDestination,
                    actualWildDestination - nominalWildDestination,
                    targetCoverDef);
                projectileThing.Launch(manningPawn, drawPos, resultingLine.Dest, semanticTarget, projectileHitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
                return true;
            }

            if (semanticTarget.Thing != null
                && semanticTarget.Thing.def.CanBenefitFromCover
                && !Rand.Chance(Mathf.Clamp01(1f - trajectoryCover.OverallBlockChance)))
            {
                Projectile projectileThing = CreateProjectileForLaunch(
                    projectileDef,
                    resultingLine.Source,
                    equipmentSource,
                    plan);
                if (projectileThing == null)
                {
                    return false;
                }

                ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                if (canHitNonTargetPawnsNow)
                {
                    projectileHitFlags |= ProjectileHitFlags.NonTargetPawns;
                }

                LocalTargetInfo coverTarget = randomCoverToMissInto != null
                    ? new LocalTargetInfo(randomCoverToMissInto)
                    : new LocalTargetInfo(resultingLine.Dest);
                LocalTargetInfo coverLaunchTarget = BdpProjectileLaunchService.FreezeCoverLaunchResolution(
                    plan, navigationTarget, semanticTarget, coverTarget,
                    coverTarget.CenterVector3, targetCoverDef);
                LogProjectileLaunchDecision(
                    "cover_intercept",
                    projectileThing,
                    plan,
                    drawPos,
                    resultingLine,
                    semanticTarget,
                    navigationTarget,
                    coverLaunchTarget,
                    semanticTarget,
                    projectileHitFlags,
                    preventFriendlyFire,
                    canHitNonTargetPawnsNow,
                    targetCoverDef,
                    forcedMissRadius,
                    accuracyFactor);
                projectileThing.Launch(manningPawn, drawPos, coverLaunchTarget, semanticTarget, projectileHitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
                return true;
            }

            ProjectileHitFlags intendedHitFlags = ProjectileHitFlags.IntendedTarget;
            if (canHitNonTargetPawnsNow)
            {
                intendedHitFlags |= ProjectileHitFlags.NonTargetPawns;
            }

            if (!semanticTarget.HasThing || semanticTarget.Thing.def.Fillage == FillCategory.Full)
            {
                intendedHitFlags |= ProjectileHitFlags.NonTargetWorld;
            }

            intendedHitFlags = ProjectileInterceptionDecision.ResolveIntendedLaunchHitFlags(
                intendedHitFlags,
                semanticTarget,
                plan != null ? plan.InteractionPolicy : null);

            if (semanticTarget.Thing != null)
            {
                Projectile projectileThing = CreateProjectileForLaunch(
                    projectileDef,
                    resultingLine.Source,
                    equipmentSource,
                    plan);
                if (projectileThing == null)
                {
                    return false;
                }

                LogProjectileLaunchDecision(
                    "intended_thing",
                    projectileThing,
                    plan,
                    drawPos,
                    resultingLine,
                    semanticTarget,
                    navigationTarget,
                    navigationTarget,
                    semanticTarget,
                    intendedHitFlags,
                    preventFriendlyFire,
                    canHitNonTargetPawnsNow,
                    targetCoverDef,
                    forcedMissRadius,
                    accuracyFactor);
                FreezeLaunchResolution(
                    plan,
                    ProjectileLaunchDisposition.IntendedTarget,
                    true,
                    ProjectileLaunchContinuationMode.IntendedRoute,
                    navigationTarget,
                    navigationTarget.CenterVector3,
                    Vector3.zero,
                    targetCoverDef);
                projectileThing.Launch(manningPawn, drawPos, navigationTarget, semanticTarget, intendedHitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
            }
            else
            {
                Projectile projectileThing = CreateProjectileForLaunch(
                    projectileDef,
                    resultingLine.Source,
                    equipmentSource,
                    plan);
                if (projectileThing == null)
                {
                    return false;
                }

                LogProjectileLaunchDecision(
                    "intended_cell",
                    projectileThing,
                    plan,
                    drawPos,
                    resultingLine,
                    semanticTarget,
                    navigationTarget,
                    resultingLine.Dest,
                    semanticTarget,
                    intendedHitFlags,
                    preventFriendlyFire,
                    canHitNonTargetPawnsNow,
                    targetCoverDef,
                    forcedMissRadius,
                    accuracyFactor);
                FreezeLaunchResolution(
                    plan,
                    ProjectileLaunchDisposition.IntendedTarget,
                    true,
                    ProjectileLaunchContinuationMode.IntendedRoute,
                    new LocalTargetInfo(resultingLine.Dest),
                    resultingLine.Dest.ToVector3Shifted(),
                    Vector3.zero,
                    targetCoverDef);
                projectileThing.Launch(manningPawn, drawPos, resultingLine.Dest, semanticTarget, intendedHitFlags, preventFriendlyFire, equipmentSource, targetCoverDef);
            }

            return true;
        }

        /// <summary>
        /// 在所有目标相关裁定完成后创建并初始化投射物。
        /// 只有进入明确的发射分支才允许生成 Thing（实体），避免射击报告失败留下未发射投射物。
        /// </summary>
        /// <param name="projectileDef">本发投射物定义。</param>
        /// <param name="spawnCell">原版投射物生成格。</param>
        /// <param name="equipmentSource">当前发射装备来源。</param>
        /// <param name="plan">当前投射物初始化计划。</param>
        /// <returns>已经完成 BDP 初始化、等待原版 Launch（发射）的投射物；生成失败时返回 null。</returns>
        private Projectile CreateProjectileForLaunch(
            ThingDef projectileDef,
            IntVec3 spawnCell,
            Thing equipmentSource,
            ProjectileInitPlan plan)
        {
            if (projectileDef == null || caster?.Map == null || !spawnCell.IsValid)
            {
                return null;
            }

            return BdpProjectileLaunchService.CreateProjectileForLaunch(
                caster,
                projectileDef,
                spawnCell,
                equipmentSource,
                plan,
                SemanticContext,
                AttackInstanceId,
                ResultId);
        }

        /// <summary>
        /// 在原版 Launch（发射）调用前冻结本发首发结果，供后续路线、碰撞和视觉阶段只读消费。
        /// </summary>
        private static void FreezeLaunchResolution(
            ProjectileInitPlan plan,
            ProjectileLaunchDisposition disposition,
            bool canHitIntendedTarget,
            ProjectileLaunchContinuationMode continuationMode,
            LocalTargetInfo actualLaunchTarget,
            Vector3 actualLaunchDestination,
            Vector3 wildMissOffset,
            ThingDef targetCoverDef)
        {
            if (plan == null)
            {
                return;
            }

            plan.LaunchResolution = new ProjectileLaunchResolution
            {
                Disposition = disposition,
                CanHitIntendedTarget = canHitIntendedTarget,
                ContinuationMode = continuationMode,
                ActualLaunchTarget = actualLaunchTarget,
                ActualLaunchDestination = actualLaunchDestination,
                WildMissOffset = wildMissOffset,
                TargetCoverDef = targetCoverDef
            };
        }

        /// <summary>
        /// 解析终端掩体的几何起点。
        /// 有路线时使用最后有效路径段起点；无路线时使用实际射击线发射源格。
        /// </summary>
        private static IntVec3 ResolveCoverOriginCell(
            ProjectileInitPlan plan,
            ShootLine resultingLine,
            LocalTargetInfo target)
        {
            if (plan != null
                && plan.HasFinalApproachStartCell
                && plan.FinalApproachStartCell.IsValid)
            {
                return plan.FinalApproachStartCell;
            }

            if (resultingLine.Source.IsValid)
            {
                return resultingLine.Source;
            }

            return target.IsValid ? target.Cell : IntVec3.Invalid;
        }

        /// <summary>
        /// 读取当前发射计划裁定后的强制失准半径。
        /// </summary>
        /// <param name="plan">当前正在发射的 projectile 初始化计划。</param>
        /// <returns>本次发射真正应使用的强制失准半径。</returns>
        private float ResolveForcedMissRadius(ProjectileInitPlan plan)
        {
            if (plan != null && plan.ForcedMissRadius > 0f)
            {
                return plan.ForcedMissRadius;
            }

            if (currentResolvedVerbSpec != null)
            {
                return currentResolvedVerbSpec.ForcedMissRadius;
            }

            return verbProps != null ? verbProps.ForcedMissRadius : 0f;
        }

        /// <summary>
        /// 读取当前发射计划裁定后的命中倍率。
        /// </summary>
        /// <param name="plan">当前正在发射的 projectile 初始化计划。</param>
        /// <returns>乘到原版命中概率上的协议真值。</returns>
        private static float ResolveAccuracyFactor(ProjectileInitPlan plan)
        {
            return plan != null && plan.AccuracyFactor > 0f
                ? plan.AccuracyFactor
                : 1f;
        }

        /// <summary>
        /// 把逐发标准概率修正转换为等效倍率，保持原版体型及姿态结算位置。
        /// 双持可能没有单一宿主会话，此时沿用已发布结果重建模块的既有机制。
        /// </summary>
        private float ResolveReportAccuracyFactor(
            ProjectileInitPlan plan, ShotReport shotReport, float accuracyFactor)
        {
            float originalChance = Mathf.Clamp01(shotReport.AimOnTargetChance_StandardTarget * accuracyFactor);
            if (originalChance <= 0f || plan == null) return accuracyFactor;

            RangedAttackModuleSession session = HostModuleSession;
            if (session == null && plan.Launcher != null
                && !string.IsNullOrWhiteSpace(plan.ResultId)
                && AttackExecutionSurfaceAccess.TryGetPublishedResult(
                    plan.Launcher, plan.ResultId, out _, out FormalExpressionResult result))
            {
                session = AttackExecutionSurfaceAccess.CreateRangedModuleSession(plan.Launcher, result);
                session?.ImportPrivateContexts(plan.AttackContextSnapshot);
            }
            if (session == null) return accuracyFactor;

            float chance = originalChance;
            IReadOnlyList<IAimChanceModifier> modifiers = session.GetAimChanceModifiers();
            for (int i = 0; i < modifiers.Count; i++)
            {
                AimChanceContext context = new AimChanceContext(caster, plan.ResultId, chance);
                float adjusted = modifiers[i].ModifyStandardAimChance(context);
                if (!float.IsNaN(adjusted) && !float.IsInfinity(adjusted))
                    chance = Mathf.Clamp01(adjusted);
            }
            return accuracyFactor * (chance / originalChance);
        }

        /// <summary>
        /// 把本发原版射击报告的公开精度事实冻结到正式投射物计划。
        /// 不保存随机命中分支，也不改变后续原版命中裁定。
        /// </summary>
        /// <param name="plan">当前正在发射的投射物计划。</param>
        /// <param name="shotReport">基于本发正式语义目标生成的原版射击报告。</param>
        /// <param name="accuracyFactor">协议层追加的命中倍率。</param>
        /// <param name="forcedMissRadius">本发正式使用的强制失准半径。</param>
        private static void CaptureAccuracySnapshot(
            ProjectileInitPlan plan,
            ShotReport shotReport,
            float accuracyFactor,
            float forcedMissRadius)
        {
            if (plan == null)
            {
                return;
            }

            float safeAccuracyFactor = Mathf.Max(0f, accuracyFactor);
            plan.AccuracySnapshot = new ProjectileAccuracySnapshot
            {
                IsAvailable = true,
                StandardAimChance = Mathf.Clamp01(
                    shotReport.AimOnTargetChance_StandardTarget * safeAccuracyFactor),
                IgnoringPostureAimChance = Mathf.Clamp01(
                    shotReport.AimOnTargetChance_IgnoringPosture * safeAccuracyFactor),
                PassCoverChance = Mathf.Clamp01(shotReport.PassCoverChance),
                ForcedMissRadius = Mathf.Max(0f, forcedMissRadius),
                AccuracyFactor = safeAccuracyFactor
            };
        }

        /// <summary>
        /// 记录当前 projectile（投射物）真正发射前的命中类别与目标真值。
        /// 这条日志只服务速度/碰撞定位，不参与任何判定。
        /// </summary>
        /// <param name="branch">当前发射分支名称。</param>
        /// <param name="plan">当前发射计划。</param>
        /// <param name="drawPos">当前真正发射原点。</param>
        /// <param name="usedTarget">当前真正喂给原版 Launch 的 usedTarget（物理飞行目标）。</param>
        /// <param name="intendedTarget">当前真正喂给原版 Launch 的 intendedTarget（意图目标）。</param>
        /// <param name="hitFlags">当前真正使用的 ProjectileHitFlags（命中类别）。</param>
        /// <param name="preventFriendlyFire">当前是否阻止友军误伤。</param>
        /// <param name="canHitNonTargetPawns">当前是否允许命中非目标 Pawn（角色）。</param>
        /// <param name="targetCoverDef">当前掩体定义。</param>
        /// <param name="forcedMissRadius">当前强制失准半径。</param>
        /// <param name="accuracyFactor">当前命中倍率。</param>
        /// <summary>
        /// 记录当前射击窗口的战斗日志入口。
        /// 它属于一次宿主发射会话，而不是某个具体业务模块。
        /// </summary>
        private void LogProjectileLaunchDecision(
            string branch,
            Projectile projectileThing,
            ProjectileInitPlan plan,
            Vector3 drawPos,
            ShootLine resultingLine,
            LocalTargetInfo semanticTarget,
            LocalTargetInfo navigationTarget,
            LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget,
            ProjectileHitFlags hitFlags,
            bool preventFriendlyFire,
            bool canHitNonTargetPawns,
            ThingDef targetCoverDef,
            float forcedMissRadius,
            float accuracyFactor)
        {
            string attackId = plan != null && !string.IsNullOrWhiteSpace(plan.AttackInstanceId)
                ? plan.AttackInstanceId
                : AttackInstanceId;
            string resultId = plan != null && !string.IsNullOrWhiteSpace(plan.ResultId)
                ? plan.ResultId
                : ResultId;
            ProjectileFlightPathSnapshot initialFlightPath = plan != null
                ? plan.InitialFlightPathSnapshot
                : null;
            BdpDiagnostics.AttackExecution(
                "event=projectile_launch_decision"
                + ", attackId=" + SafeDiagnosticText(attackId)
                + ", resultId=" + SafeDiagnosticText(resultId)
                + ", branch=" + SafeDiagnosticText(branch)
                + ", emitIndex=" + (plan != null ? plan.EmitIndex.ToString() : "-1")
                + ", projectile=" + DescribeThingForDiagnostic(projectileThing)
                + ", projectileDef=" + DescribeThingDefForDiagnostic(projectileThing != null ? projectileThing.def : null)
                + ", launchOrigin=" + drawPos
                + ", shootLineSource=" + resultingLine.Source
                + ", shootLineDest=" + resultingLine.Dest
                + ", sessionTarget=" + DescribeTargetForDiagnostic(sessionTarget)
                + ", currentTarget=" + DescribeTargetForDiagnostic(currentTarget)
                + ", planLaunchTarget=" + DescribeTargetForDiagnostic(plan != null ? plan.LaunchTarget : LocalTargetInfo.Invalid)
                + ", planAimTarget=" + DescribeTargetForDiagnostic(plan != null ? plan.AimTarget : LocalTargetInfo.Invalid)
                + ", planCurrentTarget=" + DescribeTargetForDiagnostic(plan != null ? plan.CurrentTarget : LocalTargetInfo.Invalid)
                + ", semanticTarget=" + DescribeTargetForDiagnostic(semanticTarget)
                + ", navigationTarget=" + DescribeTargetForDiagnostic(navigationTarget)
                + ", usedTarget=" + DescribeTargetForDiagnostic(usedTarget)
                + ", intendedTarget=" + DescribeTargetForDiagnostic(intendedTarget)
                + ", hitFlags=" + DescribeHitFlagsForDiagnostic(hitFlags)
                + ", targetCoverDef=" + DescribeThingDefForDiagnostic(targetCoverDef)
                + ", preventFriendlyFire=" + preventFriendlyFire
                + ", canHitNonTargetPawns=" + canHitNonTargetPawns
                + ", forcedMissRadius=" + forcedMissRadius.ToString("F3")
                + ", accuracyFactor=" + accuracyFactor.ToString("F3")
                + ", aimChanceStandard=" + (plan != null && plan.AccuracySnapshot != null
                    ? plan.AccuracySnapshot.StandardAimChance.ToString("F3") : "<none>")
                + ", aimChanceIgnoringPosture=" + (plan != null && plan.AccuracySnapshot != null
                    ? plan.AccuracySnapshot.IgnoringPostureAimChance.ToString("F3") : "<none>")
                + ", trajectoryCoverBlockChance=" + (plan != null && plan.TrajectoryCover != null
                    ? plan.TrajectoryCover.OverallBlockChance.ToString("F3") : "<none>")
                + ", trajectoryCoverPassChance=" + (plan != null && plan.AccuracySnapshot != null
                    ? plan.AccuracySnapshot.PassCoverChance.ToString("F3") : "<none>")
                + ", coverOriginCell=" + (plan != null && plan.HasFinalApproachStartCell
                    ? plan.FinalApproachStartCell.ToString() : IntVec3.Invalid.ToString())
                + ", semanticDistance=" + (caster != null && semanticTarget.IsValid
                    ? (semanticTarget.Cell - caster.Position).LengthHorizontal.ToString("F1") : "<none>")
                + ", configuredAccuracyLong=" + (plan != null && plan.HasAccuracy
                    ? plan.AccuracyLong.ToString("F3") : verbProps.accuracyLong.ToString("F3"))
                 + ", hasInitialFlightPath=" + (initialFlightPath != null)
                 + ", initialFlightPath=" + DescribeFlightPathForDiagnostic(initialFlightPath));
        }

        private static string DescribeThingDefForDiagnostic(ThingDef thingDef)
        {
            return thingDef != null ? thingDef.defName : "<none>";
        }

        private static string DescribeHitFlagsForDiagnostic(ProjectileHitFlags hitFlags)
        {
            return hitFlags.ToString();
        }

        private static string DescribeTargetForDiagnostic(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "<invalid>";
            }

            if (!target.HasThing || target.Thing == null)
            {
                return "cell=" + target.Cell;
            }

            Thing thing = target.Thing;
            string drawPos = thing.Spawned
                ? thing.DrawPos.ToString()
                : "<unspawned>";
            return thing.ThingID
                + "|cell=" + target.Cell
                + "|drawPos=" + drawPos;
        }

        private static string DescribeFlightPathForDiagnostic(ProjectileFlightPathSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "<none>";
            }

            return snapshot.Kind
                + "|start=" + snapshot.Start
                + "|controlA=" + snapshot.ControlA
                + "|controlB=" + snapshot.ControlB
                + "|end=" + snapshot.End
                + "|length=" + snapshot.ApproximateLength.ToString("F3");
        }

        private static string DescribeThingForDiagnostic(Thing thing)
        {
            if (thing == null)
            {
                return "<none>";
            }

            return thing.ThingID + "|" + DescribeThingDefForDiagnostic(thing.def);
        }

        private static string SafeDiagnosticText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
        }

        private void LogWarmupBattleEntry()
        {
            // currentTarget 是原版姿态焦点，毒蛇可能把它设为第一锚点；战斗日志仍读取最终语义目标。
            LocalTargetInfo semanticTarget = sessionTarget.IsValid ? sessionTarget : currentTarget;
            Thing targetThing = semanticTarget.HasThing ? semanticTarget.Thing : null;
            bool burst = ResolveRemainingProjectileCount() > 1;
            BattleLogEntry_RangedFire battleLogEntry = BattleLogEntryFactoryAccess.CreateRangedFire(
                caster,
                targetThing,
                ResolveVanillaBattleLogWeaponDef(),
                Projectile,
                burst,
                SemanticContext);
            Find.BattleLog.Add(battleLogEntry);
        }

        /// <summary>
        /// 只把原版能安全消费的 weaponDef 继续传给原版战斗日志。
        /// </summary>
        private ThingDef ResolveVanillaBattleLogWeaponDef()
        {
            ThingDef weaponDef = EquipmentSource?.def;
            return weaponDef != null && !weaponDef.Verbs.NullOrEmpty() ? weaponDef : null;
        }

        /// <summary>
        /// 沿用原版射击经验结算。
        /// 经验按一次宿主发射会话结算，而不是按协议业务模块结算。
        /// </summary>
        private void GrantShootingExperience()
        {
            // 射击经验按最终语义目标结算，不把第一锚点误当成 Pawn（角色）目标。
            LocalTargetInfo semanticTarget = sessionTarget.IsValid ? sessionTarget : currentTarget;
            Pawn targetPawn = semanticTarget.Thing as Pawn;
            if (targetPawn != null
                && !targetPawn.Downed
                && !targetPawn.IsColonyMech
                && CasterIsPawn
                && CasterPawn.skills != null)
            {
                float xp = targetPawn.HostileTo(caster) ? 170f : 20f;
                float cycleTime = verbProps.AdjustedFullCycleTime(this, CasterPawn);
                CasterPawn.skills.Learn(SkillDefOf.Shooting, xp * cycleTime);
            }
        }

        private new IntVec3 GetForcedMissTarget(float forcedMissRadius)
        {
            int maxExclusive = GenRadial.NumCellsInRadius(forcedMissRadius);
            int num = Rand.Range(0, maxExclusive);
            return currentTarget.Cell + GenRadial.RadialPattern[num];
        }

        /// <summary>
        /// 读取当前正式发射计划的最终预期发射数量。
        /// </summary>
        private int ResolveExpectedOriginSpreadEmitCount()
        {
            RangedVerbEmissionPlan emissionPlan = emissionCursor.PendingVerbEmissionPlan;
            if (emissionPlan != null && emissionPlan.ExpectedEmitCount > 0)
            {
                return emissionPlan.ExpectedEmitCount;
            }

            int remainingCount = emissionCursor.ResolveRemainingProjectileCount();
            return remainingCount > 0 ? remainingCount : 1;
        }

        /// <summary>
        /// 为当前正式发射计划确定一次首发横向侧别。
        /// </summary>
        private bool ResolveOriginSpreadStartsRight()
        {
            RangedVerbEmissionPlan emissionPlan = emissionCursor.PendingVerbEmissionPlan;
            if (!originSpreadSideInitialized || originSpreadSidePlan != emissionPlan)
            {
                originSpreadSidePlan = emissionPlan;
                originSpreadStartsRight = Rand.Bool;
                originSpreadSideInitialized = true;
            }

            return originSpreadStartsRight;
        }

        /// <summary>
        /// 解算当前发射计划应使用的横向随机偏移。
        /// 多发时按 EmitSequence（发射序号）交替左右，偏移幅度仍保持随机。
        /// </summary>
        private static float ResolveAlternatingLateralOffset(
            ProjectileInitPlan plan,
            bool startsRight)
        {
            float leftMax = Mathf.Max(0f, -plan.OriginSpreadLateralMin);
            float rightMax = Mathf.Max(0f, plan.OriginSpreadLateralMax);
            int sequenceParity = plan.EmitSequence % 2;
            bool rightSide = startsRight ? sequenceParity == 0 : sequenceParity != 0;

            if (rightSide)
            {
                if (rightMax > 0f)
                {
                    return Rand.Range(0f, rightMax);
                }

                return -Rand.Range(0f, leftMax);
            }

            if (leftMax > 0f)
            {
                return -Rand.Range(0f, leftMax);
            }

            return Rand.Range(0f, rightMax);
        }

        /// <summary>
        /// 按真正发射时的 source/target 几何解算真实发射点随机偏移。
        /// </summary>
        private Vector3 ResolveRandomOriginSpreadOffset(
            Vector3 source,
            LocalTargetInfo target,
            ProjectileInitPlan plan,
            int expectedEmitCount)
        {
            if (plan == null
                || !plan.HasOriginSpreadRange
                || expectedEmitCount <= 1
                || !target.IsValid)
            {
                return Vector3.zero;
            }

            Vector3 shootDir = (target.CenterVector3 - source).normalized;
            if (shootDir == Vector3.zero)
            {
                return Vector3.zero;
            }

            Vector3 rightDir = Vector3.Cross(Vector3.up, shootDir).normalized;
            if (rightDir == Vector3.zero)
            {
                rightDir = Vector3.right;
            }

            float lateralOffset = ResolveAlternatingLateralOffset(
                plan,
                ResolveOriginSpreadStartsRight());
            float forwardOffset = Rand.Range(plan.OriginSpreadForwardMin, plan.OriginSpreadForwardMax);
            return rightDir * lateralOffset + shootDir * forwardOffset;
        }

        /// <summary>
        /// 为下一轮持续攻击重新准备远程协议结果。
        /// 这里仍然通过 AttackExecution 取上游正式入口，再让远程协议生成新的宿主发射计划。
        /// </summary>
        private bool TryPreparePendingEmission(LocalTargetInfo target)
        {
            ResolveExecutionRequestRouting(out AttackExecutionReason reason, out AttackDispatchIntent dispatchIntent);
            return PrepareContinuation(target, reason, dispatchIntent);
        }

        /// <summary>
        /// 按显式的执行请求路由重建当前宿主的续射计划。
        /// 这条入口同时服务自动续射与强制目标的手动持续推进。
        /// </summary>
        internal bool PrepareContinuation(
            LocalTargetInfo target,
            AttackExecutionReason reason,
            AttackDispatchIntent dispatchIntent)
        {
            return continuationPlanner.TryPreparePendingEmission(this, target, reason, dispatchIntent);
        }

        /// <summary>
        /// 按当前 live job 语境解析这次 plan 重建应走手动还是自动入口。
        /// 这样读档续接不会把原本的手动持续攻击误降成自动攻击。
        /// </summary>
        private void ResolveExecutionRequestRouting(out AttackExecutionReason reason, out AttackDispatchIntent dispatchIntent)
        {
            reason = AttackExecutionReason.AutoRanged;
            dispatchIntent = AttackDispatchIntent.AutoAttackOrder;

            Job currentJob = CasterPawn?.jobs?.curJob;
            if (currentJob != null && currentJob.def == AttackExecutionJobDefs.RangedAttackExecution)
            {
                reason = AttackExecutionReason.Manual;
                dispatchIntent = AttackDispatchIntent.ForceTargetOrder;
            }
        }

        public override void Reset()
        {
            AttackSessionToken previousSessionToken = HostSessionToken != null ? HostSessionToken.Clone() : null;
            LogSessionClearedIfNeeded("verb_reset");
            base.Reset();
            AttackExecutionVisualRuntimeBridge.Clear(CasterPawn, previousSessionToken);
            AttackInstanceId = null;
            ResultId = null;
            HostSessionToken = null;
            HostModuleSession = null;
            hostAttackContextSnapshot = null;
            ClearStagedEntryModuleSession();
            sessionTarget = LocalTargetInfo.Invalid;
            targetLifecycleChanged = false;
            SemanticContext = null;
            currentResolvedVerbSpec = null;
            ResetInsufficientTrionPromptLatch();
            ResetOriginSpreadSideState();
            ClearPendingEmissionPlan();
        }

        /// <summary>
        /// 清空正式发射计划的横向首发侧别缓存。
        /// </summary>
        private void ResetOriginSpreadSideState()
        {
            originSpreadSideInitialized = false;
            originSpreadSidePlan = null;
            originSpreadStartsRight = false;
        }

        /// <summary>
        /// 当宿主会话真值即将被清空时输出一条诊断日志。
        /// 只在存在可疑运行态时记录，避免普通空壳 Reset 刷屏。
        /// </summary>
        private void LogSessionClearedIfNeeded(string reason)
        {
            if (HostSessionToken == null
                && string.IsNullOrWhiteSpace(AttackInstanceId)
                && string.IsNullOrWhiteSpace(ResultId)
                && !sessionTarget.IsValid
                && !HasPendingEmissionPlan())
            {
                return;
            }

            AttackExecutionDiagnostics.LogVerbSessionCleared(
                CasterPawn,
                this,
                HostSessionToken,
                AttackInstanceId,
                ResultId,
                sessionTarget,
                HasPendingEmissionPlan(),
                reason);
        }

        /// <summary>
        /// 判断当前远程 formal host 是否仍需要留在活跃 tick 队列中。
        /// 远程宿主只要仍在暖机、burst，或仍持有待消费的发射计划，就不能从运行时活跃集合里移除。
        /// </summary>
        internal bool RequiresFormalHostRuntimeTick()
        {
            return WarmingUp || Bursting || HasPendingEmissionPlan();
        }

        /// <summary>
        /// 接收目标生命周期变化，并只标记自动远程宿主需要收口。
        /// 手动强制攻击由自己的 Job（任务）生命周期负责，不能被这里提前打断。
        /// </summary>
        internal void NotifyTargetLifecycleChanged(Pawn target)
        {
            if (target == null
                || !sessionTarget.HasThing
                || sessionTarget.Thing != target
                || CasterPawn?.CurJob?.def == AttackExecutionJobDefs.RangedAttackExecution)
            {
                return;
            }

            targetLifecycleChanged = true;
            if (!RequiresFormalHostRuntimeTick())
            {
                Reset();
            }
        }

        /// <summary>
        /// 判断当前远程宿主是否已经完成本轮活动，可以安全清理目标生命周期残留。
        /// </summary>
        internal bool ShouldResetAfterTargetLifecycleChange()
        {
            return targetLifecycleChanged && !RequiresFormalHostRuntimeTick();
        }

        /// <summary>
        /// 当前是否已绑定一份仍可消费的正式宿主发射计划。
        /// </summary>
        private bool HasPendingEmissionPlan()
        {
            return emissionCursor.HasPendingEmissionPlan();
        }

        /// <summary>
        /// 尝试读取当前要消费的发射窗口。
        /// </summary>
        private bool TryGetCurrentWindow(out RangedVerbEmissionWindowPlan window)
        {
            return emissionCursor.TryGetCurrentWindow(out window);
        }

        /// <summary>
        /// 只读窥视当前待消费的第一条 projectile 初始化计划目标。
        /// 这条入口只服务诊断，不推进游标。
        /// </summary>
        private void ResolvePreparedPlanDiagnosticTargets(
            out LocalTargetInfo launchTarget,
            out LocalTargetInfo aimTarget,
            out LocalTargetInfo currentPlanTarget)
        {
            launchTarget = LocalTargetInfo.Invalid;
            aimTarget = LocalTargetInfo.Invalid;
            currentPlanTarget = LocalTargetInfo.Invalid;

            IReadOnlyList<RangedVerbEmissionWindowPlan> windows = emissionCursor.PendingEmissionWindows;
            if (windows == null
                || emissionCursor.PendingWindowIndex < 0
                || emissionCursor.PendingWindowIndex >= windows.Count)
            {
                return;
            }

            RangedVerbEmissionWindowPlan window = windows[emissionCursor.PendingWindowIndex];
            if (window?.ProjectilePlans == null
                || emissionCursor.PendingWindowProjectilePlanIndex < 0
                || emissionCursor.PendingWindowProjectilePlanIndex >= window.ProjectilePlans.Count)
            {
                return;
            }

            ProjectileInitPlan plan = window.ProjectilePlans[emissionCursor.PendingWindowProjectilePlanIndex];
            if (plan == null)
            {
                return;
            }

            launchTarget = plan.LaunchTarget;
            aimTarget = plan.AimTarget;
            currentPlanTarget = plan.CurrentTarget;
        }

        /// <summary>
        /// 验证当前发射窗口内所有计划的语义实体目标仍可交给原版射击报告。
        /// 格子目标不要求 Thing（实体）；实体目标必须仍在当前地图且保持已生成状态。
        /// </summary>
        /// <returns>当前窗口可以安全进入 Trion 提交和投射物发射时返回 true。</returns>
        private bool TryValidateCurrentEmissionWindowTargets()
        {
            if (!TryGetCurrentWindow(out RangedVerbEmissionWindowPlan window)
                || window.ProjectilePlans == null
                || window.ProjectilePlans.Count == 0)
            {
                Reset();
                return false;
            }

            int firstPlanIndex = emissionCursor.PendingWindowProjectilePlanIndex;
            for (int i = firstPlanIndex; i < window.ProjectilePlans.Count; i++)
            {
                ProjectileInitPlan plan = window.ProjectilePlans[i];
                if (plan == null)
                {
                    Reset();
                    return false;
                }

                LocalTargetInfo semanticTarget = plan.CurrentTarget.IsValid
                    ? plan.CurrentTarget
                    : plan.AimTarget.IsValid
                    ? plan.AimTarget
                    : currentTarget;
                if (!IsLiveEmissionTarget(semanticTarget))
                {
                    // 目标已经失效时关闭整条宿主会话，避免下一次姿态 tick（游戏刻）重复重建并消费资源。
                    Reset();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断一个远程发射语义目标是否仍满足原版 Thing 访问前提。
        /// </summary>
        /// <param name="target">待验证的目标。</param>
        /// <returns>格子目标或当前地图内的已生成实体目标返回 true。</returns>
        private bool IsLiveEmissionTarget(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return false;
            }

            if (!target.HasThing)
            {
                return true;
            }

            Thing thing = target.Thing;
            return thing != null
                && thing.Spawned
                && !thing.Destroyed
                && thing.Map == caster.Map;
        }

        /// <summary>
        /// 对诊断链安全探测一条目标当前是否存在原版 shoot line。
        /// 不让日志本身影响正式行为。
        /// </summary>
        private bool TryResolveShootLineDiagnostic(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return false;
            }

            if (target.HasThing && target.Thing.Map != caster.Map)
            {
                return false;
            }

            return TryFindShootLineFromTo(caster.Position, target, out _);
        }

        /// <summary>
        /// 绑定当前窗口中的下一条待发射 projectile 初始化计划。
        /// </summary>
        private bool TryBindNextWindowPlan(out ProjectileInitPlan plan)
        {
            if (!emissionCursor.TryBindNextWindowPlan(out plan) || plan == null)
            {
                return false;
            }

            AttackInstanceId = plan.AttackInstanceId;
            ResultId = plan.ResultId;
            SemanticContext = plan.SemanticContext ?? SemanticContext;
            return true;
        }

        /// <summary>
        /// 读取当前已绑定计划中还剩多少个宿主发射窗口。
        /// </summary>
        private int ResolveRemainingWindowCount()
        {
            return emissionCursor.ResolveRemainingWindowCount();
        }

        /// <summary>
        /// 读取当前已绑定计划中还剩多少条投射计划。
        /// 这只用于日志和 battle log，不驱动 burst 节奏。
        /// </summary>
        private int ResolveRemainingProjectileCount()
        {
            return emissionCursor.ResolveRemainingProjectileCount();
        }

        /// <summary>
        /// 清空当前正式宿主发射计划绑定。
        /// 同步发射窗口完成后必须整体清空，避免被下一轮误当成残余 burst。
        /// </summary>
        private void ClearPendingEmissionPlan()
        {
            emissionCursor.ClearPendingEmissionPlan();
            roundState.Reset();
        }

        /// <summary>
        /// 对内暴露当前宿主清空已准备发射状态的入口。
        /// </summary>
        internal void ClearPreparedEmissionState()
        {
            ClearPendingEmissionPlan();
        }

        private bool TryEnsureRoundTrionAdmission()
        {
            if (roundState.TryEnsureRoundTrionAdmission(CasterPawn, ShowInsufficientTrionMessage))
            {
                ResetInsufficientTrionPromptLatch();
                return true;
            }

            return false;
        }

        private bool TryCommitRoundTrionBeforeFirstEmission()
        {
            if (roundState.TryCommitRoundTrionBeforeFirstEmission(CasterPawn, ShowInsufficientTrionMessage))
            {
                ResetInsufficientTrionPromptLatch();
                return true;
            }

            state = VerbState.Idle;
            ClearPendingEmissionPlan();
            return false;
        }

        private void ShowInsufficientTrionMessage(RangedAttackTrionGateResult result)
        {
            if (HasInsufficientTrionPromptLatch())
            {
                return;
            }

            string message = result != null && !string.IsNullOrWhiteSpace(result.Message)
                ? result.Message
                : "BDP_Message_Trion_RangedInsufficient".Translate().ToString();
            MarkInsufficientTrionPromptLatch();
            Messages.Message(message, CasterPawn, MessageTypeDefOf.RejectInput, false);
        }

        private bool HasInsufficientTrionPromptLatch()
        {
            SyncInsufficientTrionPromptLatchToCurrentSession();
            string currentSessionKey = ResolveInsufficientTrionPromptSessionKey();
            return !string.IsNullOrWhiteSpace(currentSessionKey)
                && insufficientTrionPromptLatchedAttackInstanceId == currentSessionKey;
        }

        private void MarkInsufficientTrionPromptLatch()
        {
            SyncInsufficientTrionPromptLatchToCurrentSession();
            string currentSessionKey = ResolveInsufficientTrionPromptSessionKey();
            if (!string.IsNullOrWhiteSpace(currentSessionKey))
            {
                insufficientTrionPromptLatchedAttackInstanceId = currentSessionKey;
            }
        }

        private void ResetInsufficientTrionPromptLatch()
        {
            insufficientTrionPromptLatchedAttackInstanceId = null;
        }

        private void SyncInsufficientTrionPromptLatchToCurrentSession()
        {
            string currentSessionKey = ResolveInsufficientTrionPromptSessionKey();
            if (string.IsNullOrWhiteSpace(currentSessionKey))
            {
                insufficientTrionPromptLatchedAttackInstanceId = null;
                return;
            }

            if (!string.IsNullOrWhiteSpace(insufficientTrionPromptLatchedAttackInstanceId)
                && insufficientTrionPromptLatchedAttackInstanceId != currentSessionKey)
            {
                insufficientTrionPromptLatchedAttackInstanceId = null;
            }
        }

        private string ResolveInsufficientTrionPromptSessionKey()
        {
            if (HostSessionToken != null
                && !string.IsNullOrWhiteSpace(HostSessionToken.AttackInstanceId))
            {
                return HostSessionToken.AttackInstanceId;
            }

            return !string.IsNullOrWhiteSpace(AttackInstanceId)
                ? AttackInstanceId
                : null;
        }

        /// <summary>
        /// 判断当前持久化下来的 burst cursor 是否仍然处于最小合法区间。
        /// 真正的窗口上界会在惰性重建 plan 后再校验。
        /// </summary>
        protected bool HasValidLoadedBurstCursor()
        {
            return emissionCursor.HasValidLoadedBurstCursor();
        }

        /// <summary>
        /// 输出当前宿主对正式发射计划的消费摘要。
        /// 这里记录的是宿主执行结果，不是业务模块结果。
        /// </summary>
        private void LogCurrentVerbEmissionSummary()
        {
            if (emissionCursor.PendingVerbEmissionPlan == null)
            {
                return;
            }

            bool stepCompleted = emissionCursor.PendingEmissionConsumedCount >= emissionCursor.PendingVerbEmissionPlan.ExpectedEmitCount;
            AttackExecutionDiagnostics.LogVerbEmissionSummary(
                CasterPawn,
                this,
                emissionCursor.PendingVerbEmissionPlan,
                emissionCursor.PendingEmissionConsumedCount,
                stepCompleted,
                currentTarget);
        }

        /// <summary>
        /// 当新起手请求发现当前壳里还挂着旧目标的待发射计划时，输出一条异常日志。
        /// 同目标的正常重建不记日志，避免持续攻击期间重复刷屏。
        /// </summary>
        private void LogStalePendingEmissionPlanIfNeeded(LocalTargetInfo requestedTarget)
        {
            if (!TryGetFirstPendingLaunchTarget(out LocalTargetInfo previousTarget)
                || TargetsEquivalent(previousTarget, requestedTarget))
            {
                return;
            }

            AttackExecutionDiagnostics.LogStalePendingEmissionPlanCleared(
                CasterPawn,
                this,
                emissionCursor.PendingVerbEmissionPlan,
                emissionCursor.PendingWindowIndex,
                emissionCursor.PendingWindowProjectilePlanIndex,
                emissionCursor.PendingEmissionConsumedCount,
                previousTarget,
                requestedTarget);
        }

        /// <summary>
        /// 当刚准备好的首发目标和本次请求目标不一致时，输出一条异常日志。
        /// 这条日志只记录事实，不在这里推断业务正确性。
        /// </summary>
        private void LogPreparedTargetMismatchIfNeeded(LocalTargetInfo requestedTarget)
        {
            if (!TryGetFirstPendingLaunchTarget(out LocalTargetInfo preparedTarget)
                || TargetsEquivalent(preparedTarget, requestedTarget))
            {
                return;
            }

            AttackExecutionDiagnostics.LogPreparedTargetMismatch(
                CasterPawn,
                this,
                HostResultId,
                requestedTarget,
                preparedTarget);
        }

        /// <summary>
        /// 把远程协议生成的首段 launch target 适配成原版起手/暖机所需的 cast target。
        /// 若首段目标与业务请求目标一致，或协议未给出有效 launch target，则继续沿用原请求目标。
        /// </summary>
        private LocalTargetInfo ResolveBaseVerbStartTarget(
            LocalTargetInfo requestedTarget,
            LocalTargetInfo preparedLaunchTarget)
        {
            if (!preparedLaunchTarget.IsValid || TargetsEquivalent(preparedLaunchTarget, requestedTarget))
            {
                return requestedTarget;
            }

            return preparedLaunchTarget;
        }

        /// <summary>
        /// 读取当前待消费窗口中下一条真正会被发出去的 launch target。
        /// </summary>
        private bool TryGetFirstPendingLaunchTarget(out LocalTargetInfo target)
        {
            return emissionCursor.TryGetFirstPendingLaunchTarget(out target);
        }

        /// <summary>
        /// 判断两个目标在诊断层面是否可视为同一个目标。
        /// 如果目标 Thing 相同，或者都落在同一格，就不把它当成异常差异。
        /// </summary>
        private static bool TargetsEquivalent(LocalTargetInfo left, LocalTargetInfo right)
        {
            if (!left.IsValid || !right.IsValid)
            {
                return !left.IsValid && !right.IsValid;
            }

            if (left.HasThing && right.HasThing)
            {
                return left.Thing == right.Thing;
            }

            return left.Cell == right.Cell;
        }

        /// <summary>
        /// 把 ThingDef（物体定义）压成适合诊断日志的一行值。
        /// </summary>
        /// <param name="thingDef">待描述定义。</param>
        /// <returns>诊断友好的定义文本。</returns>
        /// <summary>
        /// 把 ProjectileHitFlags（投射物命中类别）压成适合诊断日志的一行值。
        /// </summary>
        /// <param name="hitFlags">待描述命中类别。</param>
        /// <returns>诊断友好的命中类别文本。</returns>
    }
}
