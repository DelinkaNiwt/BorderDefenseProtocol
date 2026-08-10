using BDP.Core.AttackExecution;
using BDP.Core.CombatBodySession;
using BDP.Core.Combos;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Projection;
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

            if (!combatBodySessionPolicy.ShouldPublishCombatProjection(ownerPawn, owner))
            {
                Publish(
                    TriggerCombatProjectionState.CreateEmpty(currentProjectionVersion + 1),
                    TriggerPresentationState.CreateEmpty(currentProjectionVersion + 1),
                    ownerPawn,
                    allowLoadedProjectionRebind);
                return true;
            }

            ExpressionService expressionService = owner.RuntimeServices != null
                ? owner.RuntimeServices.ExpressionService
                : null;
            TriggerProjectionBuildInput buildInput = owner.BuildProjectionBuildInput();
            int nextProjectionVersion = currentProjectionVersion + 1;
            TriggerCombatProjectionState combatProjection =
                combatProjectionBuilder.Build(buildInput, ownerPawn, expressionService, nextProjectionVersion);
            ExpressionSnapshot snapshot = combatProjection != null ? combatProjection.Snapshot : null;
            if (owner.HasHeldChipsInFormalContainer
                && (snapshot == null || snapshot.Results == null || snapshot.Results.Count == 0))
            {
                BdpDiagnostics.Once(
                    "trigger.expression_empty_with_nonempty_container." + (owner.parent != null ? owner.parent.ThingID : "null"),
                    "表达快照为空，但 Trigger 仍持有芯片容器内容。parent="
                    + (owner.parent != null ? owner.parent.ThingID : "null")
                    + ", ownerPawn="
                    + ownerPawn.ThingID);
            }

            TriggerPresentationState presentationProjection =
                presentationBuilder.Build(expressionService, snapshot, nextProjectionVersion);
            Publish(combatProjection, presentationProjection, ownerPawn, allowLoadedProjectionRebind);
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
            owner?.RuntimeServices?.TriggerVisualRuntimeStateOwner?.ResetForPublishedProjection(currentProjectionVersion);

            if (ownerPawn != null)
            {
                ExpressionService expressionService = owner.RuntimeServices != null
                    ? owner.RuntimeServices.ExpressionService
                    : null;
                expressionService?.SyncProjectedHosts(ownerPawn, publishedCombatProjection.Snapshot);
            }

            comboUseRequirementNoticeTracker.Sync(
                ownerPawn,
                publishedCombatProjection.Snapshot);
            owner?.VerbHostManager?.Refresh(publishedCombatProjection);

            if (ownerPawn == null)
            {
                return;
            }

            if (allowLoadedProjectionRebind)
            {
                AttackExecutionPostLoadRecovery.RecoverStaleAttackSession(ownerPawn);
                return;
            }

            AttackExecutionPostLoadRecovery.InterruptInvalidAttackSession(ownerPawn);
        }
    }
}

