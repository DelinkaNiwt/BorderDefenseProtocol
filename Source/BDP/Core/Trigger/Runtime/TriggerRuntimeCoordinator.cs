using BDP.Core.AttackExecution;
using BDP.Core.CombatBodySession;
using System.Collections.Generic;
using BDP.Core.Combos;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Projection;
using BDP.Core.Verbs;
using BDP.Core.VerbHosting;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// Trigger 运行时发布协调器。
    /// 它是已发布战斗/表现投影的唯一 owner，负责 dirty、重建和发布收口。
    /// </summary>
    internal sealed class TriggerRuntimeCoordinator
    {
        /// <summary>
        /// 当前协调器所属的 TriggerBody owner。
        /// </summary>
        private readonly CompTriggerBody owner;

        /// <summary>
        /// 当前已发布投影版本号。
        /// </summary>
        private int currentProjectionVersion;

        /// <summary>
        /// 当前已发布的战斗投影。
        /// </summary>
        private TriggerCombatProjectionState currentCombatProjection;

        /// <summary>
        /// 当前已发布的表现投影。
        /// </summary>
        private TriggerPresentationState currentPresentationProjection;

        /// <summary>
        /// 当前是否存在待发布的脏变更。
        /// </summary>
        private bool projectionDirty;

        /// <summary>
        /// 当前待发布脏变更的最后来源。
        /// </summary>
        private ProjectionDirtyReason dirtyReason;

        /// <summary>
        /// Trigger 战斗投影装配器。
        /// </summary>
        private readonly TriggerCombatProjectionBuilder combatProjectionBuilder;

        /// <summary>
        /// Trigger 表现投影装配器。
        /// </summary>
        private readonly TriggerPresentationBuilder presentationBuilder;

        /// <summary>
        /// Combo 使用条件的低频错峰监视器。
        /// </summary>
        private readonly ComboUseRequirementMonitor comboUseRequirementMonitor;

        /// <summary>
        /// 当前 Trigger 会话自己的 Combo 受阻提示锁存器。
        /// </summary>
        private readonly ComboUseRequirementNoticeTracker comboUseRequirementNoticeTracker;

        /// <summary>
        /// 战斗会话发布裁定策略。
        /// </summary>
        private static readonly CombatBodySessionPolicy combatBodySessionPolicy = new CombatBodySessionPolicy();

        /// <summary>
        /// 用指定 owner 构造运行时协调器。
        /// </summary>
        public TriggerRuntimeCoordinator(CompTriggerBody owner)
        {
            this.owner = owner;
            currentProjectionVersion = 0;
            currentCombatProjection = TriggerCombatProjectionState.CreateEmpty(currentProjectionVersion);
            currentPresentationProjection = TriggerPresentationState.CreateEmpty(currentProjectionVersion);
            projectionDirty = false;
            dirtyReason = ProjectionDirtyReason.None;
            combatProjectionBuilder = new TriggerCombatProjectionBuilder();
            presentationBuilder = new TriggerPresentationBuilder();
            comboUseRequirementMonitor = new ComboUseRequirementMonitor();
            comboUseRequirementNoticeTracker = new ComboUseRequirementNoticeTracker();
        }

        /// <summary>
        /// 读取当前已发布的战斗投影。
        /// </summary>
        internal TriggerCombatProjectionState CurrentCombatProjection
        {
            get { return currentCombatProjection; }
        }

        /// <summary>
        /// 读取当前已发布的表现投影。
        /// </summary>
        internal TriggerPresentationState CurrentPresentationProjection
        {
            get { return currentPresentationProjection; }
        }

        /// <summary>
        /// 把当前战斗投影标记为 dirty。
        /// </summary>
        internal void MarkDirty(ProjectionDirtyReason reason)
        {
            projectionDirty = true;
            dirtyReason = reason;
        }

        /// <summary>
        /// 由主武器唯一 owner 推进一次 Trigger 运行时。
        /// 顺序固定为：primary owner 守卫、post-load finalize、切换结算、条件复查、正式发布、formal host tick。
        /// </summary>
        internal bool RuntimeTick()
        {
            if (owner == null || !owner.IsCurrentPrimaryRuntimeOwner())
            {
                return false;
            }

            if (!owner.TryFinalizePostLoadProjectionRefresh())
            {
                return false;
            }

            int projectionVersionBeforeSwitchResolve = currentProjectionVersion;
            if (owner.ResolveDueSwitchTransitionsForRuntimeTick()
                && currentProjectionVersion == projectionVersionBeforeSwitchResolve)
            {
                MarkDirty(ProjectionDirtyReason.SwitchTransitionResolved);
            }

            owner.SyncActivationAudioForRuntimeTick();

            owner.CheckActiveActivationRequirementsForRuntimeTick();

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int stableThingId = owner.parent != null ? owner.parent.thingIDNumber : 0;
            if (comboUseRequirementMonitor.ShouldRefresh(
                currentTick,
                stableThingId,
                owner.OwnerPawn,
                currentCombatProjection != null ? currentCombatProjection.Snapshot : null))
            {
                MarkDirty(ProjectionDirtyReason.ComboUseRequirementChanged);
            }

            if (projectionDirty && !RebuildAndPublish())
            {
                return false;
            }

            owner?.VerbHostManager?.Tick();

            return true;
        }

        /// <summary>
        /// 按当前 owner 正式真值重建并发布战斗/表现投影。
        /// </summary>
        internal bool RebuildAndPublish()
        {
            return RebuildAndPublishCore(allowLoadedProjectionRebind: false);
        }

        /// <summary>
        /// 按当前已经发布的表达快照立即同步外围表达宿主。
        /// 能力按钮改变实际 Hediff 后复用这条入口，避免重新解析 Trigger 投影或让能力组件直接操作持续消耗账本。
        /// </summary>
        internal void SynchronizePublishedExpressionHostsImmediately()
        {
            Pawn ownerPawn = owner != null ? owner.OwnerPawn : null;
            TriggerCombatProjectionState publishedProjection = currentCombatProjection;
            if (ownerPawn == null || publishedProjection == null || publishedProjection.Snapshot == null)
            {
                return;
            }

            ExpressionService expressionService = owner.RuntimeServices != null
                ? owner.RuntimeServices.ExpressionService
                : null;
            expressionService?.SyncProjectedHosts(ownerPawn, publishedProjection.Snapshot);
        }

        /// <summary>
        /// 按当前 owner 正式真值重建并发布战斗/表现投影。
        /// 可按调用场景决定发布后是中断旧会话，还是允许读档会话重绑到当前版本。
        /// </summary>
        private bool RebuildAndPublishCore(bool allowLoadedProjectionRebind)
        {
            if (!projectionDirty)
            {
                return true;
            }

            Pawn ownerPawn = owner != null ? owner.OwnerPawn : null;
            if (ownerPawn == null)
            {
                return false;
            }

            ProjectionDirtyReason rebuildReason = dirtyReason;
            long rebuildStart = BdpDiagnostics.BeginTiming();

            if (!combatBodySessionPolicy.ShouldPublishCombatProjection(ownerPawn, owner))
            {
                long emptyPublishStart = BdpDiagnostics.BeginTiming();
                Publish(
                    TriggerCombatProjectionState.CreateEmpty(currentProjectionVersion + 1),
                    TriggerPresentationState.CreateEmpty(currentProjectionVersion + 1),
                    ownerPawn,
                    allowLoadedProjectionRebind);
                BdpDiagnostics.TraceTiming(
                    "表达投影重建 | stage=empty_publish | reason=" + rebuildReason
                    + " | pawn=" + ownerPawn.ThingID,
                    emptyPublishStart);
                BdpDiagnostics.TraceTiming(
                    "表达投影重建 | stage=total | reason=" + rebuildReason
                    + " | pawn=" + ownerPawn.ThingID,
                    rebuildStart);
                return true;
            }

            ExpressionService expressionService = owner.RuntimeServices != null
                ? owner.RuntimeServices.ExpressionService
                : null;
            long buildInputStart = BdpDiagnostics.BeginTiming();
            TriggerProjectionBuildInput buildInput = owner.BuildProjectionBuildInput();
            BdpDiagnostics.TraceTiming(
                "表达投影重建 | stage=build_input | reason=" + rebuildReason
                + " | pawn=" + ownerPawn.ThingID,
                buildInputStart);
            LogProjectionStage(
                ownerPawn,
                "projection_rebuild_enter",
                "dirtyReason=" + dirtyReason
                + " | hasBuildInput=" + (buildInput != null)
                + " | hasExpressionService=" + (expressionService != null));
            int nextProjectionVersion = currentProjectionVersion + 1;
            long combatProjectionStart = BdpDiagnostics.BeginTiming();
            TriggerCombatProjectionState combatProjection =
                combatProjectionBuilder.Build(buildInput, ownerPawn, expressionService, nextProjectionVersion);
            BdpDiagnostics.TraceTiming(
                "表达投影重建 | stage=build_combat_projection | reason=" + rebuildReason
                + " | pawn=" + ownerPawn.ThingID,
                combatProjectionStart);
            LogProjectionStage(
                ownerPawn,
                "projection_rebuild_result",
                DescribeCombatProjection(combatProjection));
            ExpressionSnapshot snapshot = combatProjection != null ? combatProjection.Snapshot : null;
            if (TriggerFunctionAvailability.Allows(ownerPawn) && owner.HasHeldChipsInFormalContainer
                && (snapshot == null || snapshot.Results == null || snapshot.Results.Count == 0))
            {
                BdpDiagnostics.Once(
                    "trigger.expression_empty_with_nonempty_container." + (owner.parent != null ? owner.parent.ThingID : "null"),
                    "表达快照为空，但 Trigger 仍持有芯片容器内容。parent="
                    + (owner.parent != null ? owner.parent.ThingID : "null")
                    + ", ownerPawn="
                    + ownerPawn.ThingID);
            }

            long presentationStart = BdpDiagnostics.BeginTiming();
            TriggerPresentationState presentationProjection =
                presentationBuilder.Build(
                    expressionService,
                    snapshot,
                    buildInput != null ? buildInput.InformationInputs : null,
                    nextProjectionVersion);
            BdpDiagnostics.TraceTiming(
                "表达投影重建 | stage=build_presentation_projection | reason=" + rebuildReason
                + " | pawn=" + ownerPawn.ThingID,
                presentationStart);

            long publishStart = BdpDiagnostics.BeginTiming();
            Publish(combatProjection, presentationProjection, ownerPawn, allowLoadedProjectionRebind);
            BdpDiagnostics.TraceTiming(
                "表达投影重建 | stage=publish | reason=" + rebuildReason
                + " | pawn=" + ownerPawn.ThingID,
                publishStart);
            BdpDiagnostics.TraceTiming(
                "表达投影重建 | stage=total | reason=" + rebuildReason
                + " | pawn=" + ownerPawn.ThingID,
                rebuildStart);
            return true;
        }

        /// <summary>
        /// 在 post-load 结束时尝试完成首次正式发布。
        /// </summary>
        internal bool TryFinalizePostLoadProjectionRefresh()
        {
            if (owner == null || owner.OwnerPawn == null)
            {
                return false;
            }

            return RebuildAndPublishCore(allowLoadedProjectionRebind: true);
        }

        /// <summary>
        /// 清空当前已发布战斗/表现投影，并同步清空外围宿主。
        /// </summary>
        internal void ClearPublishedProjection(Pawn pawn)
        {
            owner?.ClearActivationAudio();
            int nextProjectionVersion = currentProjectionVersion + 1;
            Publish(
                TriggerCombatProjectionState.CreateEmpty(nextProjectionVersion),
                TriggerPresentationState.CreateEmpty(nextProjectionVersion),
                pawn,
                allowLoadedProjectionRebind: false);
        }

        /// <summary>
        /// 把新投影发布到 owner，并统一同步外围宿主与 formal host。
        /// </summary>
        private void Publish(
            TriggerCombatProjectionState combatProjection,
            TriggerPresentationState presentationProjection,
            Pawn ownerPawn,
            bool allowLoadedProjectionRebind)
        {
            // 条件变化只使受影响结果失效；保留上一版供宿主逐条比较，读档和其它换版不走此入口。
            TriggerCombatProjectionState continuityProjection = !allowLoadedProjectionRebind
                && dirtyReason == ProjectionDirtyReason.SourceConditionsChanged ? currentCombatProjection : null;
            TriggerVisualRuntimeStateOwner visualOwner = owner?.RuntimeServices?.TriggerVisualRuntimeStateOwner;
            TriggerVisualRuntimeState previousVisualState = continuityProjection != null ? visualOwner?.PublishedState : null;
            TriggerCombatProjectionState publishedCombatProjection =
                combatProjection ?? TriggerCombatProjectionState.CreateEmpty(currentProjectionVersion + 1);
            TriggerPresentationState publishedPresentationProjection =
                presentationProjection != null
                && presentationProjection.ProjectionVersion == publishedCombatProjection.ProjectionVersion
                    ? presentationProjection
                    : TriggerPresentationState.CreateEmpty(publishedCombatProjection.ProjectionVersion);
            currentProjectionVersion = publishedCombatProjection.ProjectionVersion;
            currentCombatProjection = publishedCombatProjection;
            currentPresentationProjection = publishedPresentationProjection;
            projectionDirty = false;
            dirtyReason = ProjectionDirtyReason.None;
            LogProjectionStage(
                ownerPawn,
                "projection_published",
                DescribeCombatProjection(publishedCombatProjection)
                + " | allowLoadedProjectionRebind=" + allowLoadedProjectionRebind);
            long visualStateStart = BdpDiagnostics.BeginTiming();
            owner?.RuntimeServices?.TriggerVisualRuntimeStateOwner?.ResetForPublishedProjection(currentProjectionVersion);
            BdpDiagnostics.TraceTiming(
                "表达投影发布 | stage=reset_visual_state | pawn=" + (ownerPawn != null ? ownerPawn.ThingID : "null"),
                visualStateStart);

            if (ownerPawn != null)
            {
                ExpressionService expressionService = owner.RuntimeServices != null
                    ? owner.RuntimeServices.ExpressionService
                    : null;
                long hostSyncStart = BdpDiagnostics.BeginTiming();
                expressionService?.SyncProjectedHosts(ownerPawn, publishedCombatProjection.Snapshot);
                BdpDiagnostics.TraceTiming(
                    "表达投影发布 | stage=sync_projected_hosts | pawn=" + ownerPawn.ThingID,
                    hostSyncStart);
                LogProjectionStage(
                    ownerPawn,
                    "projection_host_sync_returned",
                    "hasExpressionService=" + (expressionService != null)
                    + " | " + DescribeCombatProjection(publishedCombatProjection));
            }

            long comboNoticeStart = BdpDiagnostics.BeginTiming();
            comboUseRequirementNoticeTracker.Sync(
                ownerPawn,
                publishedCombatProjection.Snapshot);
            BdpDiagnostics.TraceTiming(
                "表达投影发布 | stage=sync_combo_notice | pawn=" + (ownerPawn != null ? ownerPawn.ThingID : "null"),
                comboNoticeStart);

            long verbHostStart = BdpDiagnostics.BeginTiming();
            ISet<Verb> rejectedSessions = owner?.VerbHostManager?.Refresh(publishedCombatProjection, continuityProjection);
            if (previousVisualState != null
                && owner.VerbHostManager.TryGetByResultId(previousVisualState.ActiveHostResultId, out BdpFormalVerbBinding visualBinding))
            {
                Verb activeVerb = visualBinding.ResolveActiveVerb();
                if (activeVerb != null && rejectedSessions != null && !rejectedSessions.Contains(activeVerb))
                {
                    AttackSessionToken token = activeVerb is BdpVerb_FormalHostShoot shoot ? shoot.HostSessionToken
                        : (activeVerb as BdpVerb_FormalHostMelee)?.HostSessionToken;
                    visualOwner?.RestoreContinuedExecutionState(previousVisualState, token, continuityProjection.ProjectionVersion);
                }
            }
            BdpDiagnostics.TraceTiming(
                "表达投影发布 | stage=refresh_verb_hosts | pawn=" + (ownerPawn != null ? ownerPawn.ThingID : "null"),
                verbHostStart);

            if (ownerPawn == null)
            {
                return;
            }

            if (allowLoadedProjectionRebind)
            {
                long recoveryStart = BdpDiagnostics.BeginTiming();
                AttackExecutionPostLoadRecovery.RecoverStaleAttackSession(ownerPawn);
                BdpDiagnostics.TraceTiming(
                    "表达投影发布 | stage=recover_post_load_attack | pawn=" + ownerPawn.ThingID,
                    recoveryStart);
                return;
            }

            long interruptStart = BdpDiagnostics.BeginTiming();
            AttackExecutionPostLoadRecovery.InterruptInvalidAttackSession(ownerPawn, rejectedSessions);
            BdpDiagnostics.TraceTiming(
                "表达投影发布 | stage=interrupt_invalid_attack | pawn=" + ownerPawn.ThingID,
                interruptStart);
        }

        /// <summary>
        /// 记录 Trigger 投影生命周期的大方向边界。
        /// 这里只观察重建、发布和宿主同步，不改变投影内容或发布时序。
        /// </summary>
        private void LogProjectionStage(Pawn pawn, string stage, string details)
        {
            if (pawn == null)
            {
                return;
            }

            BdpDiagnostics.Throttled(
                "expression.projection.lifecycle." + pawn.ThingID + "." + stage,
                "表达投影生命周期 | stage=" + stage
                + " | pawn=" + pawn.ThingID
                + " | activeSlots=" + (owner != null ? owner.DescribeAllSlotsForDiagnostics() : "none")
                + " | " + (details ?? "none"));
        }

        /// <summary>
        /// 压缩一份战斗投影的结构摘要，供大方向断点诊断使用。
        /// </summary>
        private static string DescribeCombatProjection(TriggerCombatProjectionState projection)
        {
            ExpressionSnapshot snapshot = projection != null ? projection.Snapshot : null;
            int resultCount = snapshot?.Results != null ? snapshot.Results.Count : 0;
            int abilityCount = 0;
            List<string> abilityResults = new List<string>();
            if (snapshot?.Results != null)
            {
                for (int index = 0; index < snapshot.Results.Count; index++)
                {
                    FormalExpressionResult result = snapshot.Results[index];
                    if (result == null || result.OutputChannel != ExpressionOutputChannel.Ability)
                    {
                        continue;
                    }

                    abilityCount++;
                    abilityResults.Add(
                        (result.Id ?? "null")
                        + "->"
                        + (result.AbilityDefName ?? "null"));
                }
            }

            return "projectionVersion=" + (projection != null ? projection.ProjectionVersion.ToString() : "null")
                + " | hasSnapshot=" + (snapshot != null)
                + " | resultCount=" + resultCount
                + " | abilityCount=" + abilityCount
                + " | abilityResults=" + (abilityResults.Count > 0
                    ? string.Join(",", abilityResults)
                    : "none");
        }
    }
}
