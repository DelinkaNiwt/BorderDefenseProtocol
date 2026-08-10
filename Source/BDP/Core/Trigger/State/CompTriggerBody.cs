using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.Requirements;
using BDP.Core.CombatBody;
using BDP.Core.Trigger.Projection;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Trion;
using BDP.Core.VerbHosting;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 的正式真值 owner。
    /// 它负责槽位、切换、容器和内部过程，不直接充当对外正式表面。
    /// 它也不再承担正式攻击执行入口职责。
    /// FormalHost 的内部 fallback 声明见同名 partial 文件。
    /// </summary>
    public sealed partial class CompTriggerBody : CompEquippable, IThingHolder, IVerbOwner
    {
        /// <summary>
        /// 主侧槽位。
        /// </summary>
        private List<TriggerSlotState> mainSlots;

        /// <summary>
        /// 副侧槽位。
        /// </summary>
        private List<TriggerSlotState> subSlots;

        /// <summary>
        /// 特殊侧槽位。
        /// </summary>
        private List<TriggerSlotState> specialSlots;

        /// <summary>
        /// Trigger 芯片容器。
        /// </summary>
        private ThingOwner<Thing> chipContainer;

        /// <summary>
        /// 主侧切换上下文。
        /// </summary>
        private SwitchContext mainSwitchContext;

        /// <summary>
        /// 副侧切换上下文。
        /// </summary>
        private SwitchContext subSwitchContext;

        /// <summary>
        /// 特殊侧切换上下文。
        /// </summary>
        private SwitchContext specialSwitchContext;

        /// <summary>
        /// 槽位装载变化事件。
        /// </summary>
        internal event Action<TriggerSlotStateChangedArgs> SlotLoadoutChanged;

        /// <summary>
        /// 槽位正式启用完成事件。
        /// </summary>
        internal event Action<TriggerSlotStateChangedArgs> SlotActivationCommitted;

        /// <summary>
        /// 槽位正式停用完成事件。
        /// </summary>
        internal event Action<TriggerSlotStateChangedArgs> SlotDeactivated;

        /// <summary>
        /// 槽位禁用状态变化事件。
        /// </summary>
        internal event Action<TriggerSlotStateChangedArgs> SlotDisableStateChanged;

        /// <summary>
        /// 禁用同步缓存。
        /// </summary>
        private TriggerDisableSyncCache disableSyncCache;

        /// <summary>
        /// TriggerBody 内部 FormalHost Verb 绑定管理器。
        /// formal host 只在 BDP 内部执行侧可见，不再接管原版武器声明面。
        /// </summary>
        private TriggerBodyVerbHostManager verbHostManager;

        /// <summary>
        /// Trigger 已发布战斗投影的唯一运行时 owner。
        /// </summary>
        private TriggerRuntimeCoordinator runtimeCoordinator;

        /// <summary>
        /// 当前 `Trigger` owner 专属的运行时服务根。
        /// </summary>
        private readonly TriggerRuntimeServices runtimeServices;

        /// <summary>
        /// Trigger 正式读取表面。
        /// </summary>
        private readonly TriggerLoadoutReaderSurface loadoutReaderSurface;

        /// <summary>
        /// Trigger 正式交互语义表面。
        /// </summary>
        private readonly TriggerInteractionSurface interactionSurface;

        /// <summary>
        /// Trigger 正式交互语义解释器。
        /// 它负责把 owner 内部真值解释成正式外部交互语义结果。
        /// </summary>
        private readonly TriggerInteractionInterpreter interactionInterpreter;

        /// <summary>
        /// Trigger 正式请求表面。
        /// </summary>
        private readonly TriggerLoadoutCommandSurface loadoutCommandSurface;

        /// <summary>
        /// Trigger 正式事件表面。
        /// </summary>
        private readonly TriggerEventSurface eventSurface;

        /// <summary>
        /// Trigger 正式服务入口。
        /// </summary>
        private readonly TriggerService triggerService;

        /// <summary>
        /// 当前是否处于 PostLoad 恢复阶段。
        /// 恢复阶段内只允许消费已恢复的正式真值，不允许再次触发常规运行时重算。
        /// </summary>
        private bool isRestoringPostLoad;

        /// <summary>
        /// 当前是否还有一轮待读档后补做的投影刷新。
        /// 只在 Trigger 真值尚未完全恢复时临时挂起，不引入新的长期状态机。
        /// </summary>
        private bool pendingPostLoadProjectionRefresh;

        /// <summary>
        /// 当前是否处于战斗体不可用导致的统一禁用覆盖。
        /// 它只复用正式禁用态，不额外引入新的槽位状态类别。
        /// </summary>
        private bool combatBodyUnavailableDisable;

        /// <summary>
        /// 初始化 owner 和各正式表面。
        /// 这些表面只对外暴露 Trigger 真值、交互语义和宿主同步，不承接正式攻击执行。
        /// </summary>
        public CompTriggerBody()
        {
            chipContainer = new ThingOwner<Thing>(this);
            triggerService = new TriggerService();
            loadoutReaderSurface = new TriggerLoadoutReaderSurface(this);
            interactionSurface = new TriggerInteractionSurface(this);
            interactionInterpreter = new TriggerInteractionInterpreter(
                GetSlot,
                GetActiveSlot,
                GetActiveSlotRaw,
                GetSwitchState,
                GetActiveSwitchContext,
                NormalizeDirectControlSlot,
                ShouldUseSynchronizedHandTransition,
                GetCurrentTick,
                triggerService.ResolveChipActivationDelayTicks,
                triggerService.ResolveChipDeactivationDelayTicks,
                () => CombatBodySurfaceAccess.ResolveReader(OwnerPawn)?.Phase == CombatBodyPhase.Active,
                chip => ChipActivationRequirementService.Instance.Evaluate(OwnerPawn, chip));
            verbHostManager = new TriggerBodyVerbHostManager(this);
            runtimeServices = new TriggerRuntimeServices();
            runtimeCoordinator = new TriggerRuntimeCoordinator(this);
            loadoutCommandSurface = new TriggerLoadoutCommandSurface(this);
            eventSurface = new TriggerEventSurface(this);
        }

        /// <summary>
        /// 对外统一返回 Trigger 正式读取口。
        /// </summary>
        internal ITriggerLoadoutReader LoadoutReaderSurface
        {
            get { return loadoutReaderSurface; }
        }

        /// <summary>
        /// 对外统一返回 Trigger 正式交互语义读取口。
        /// </summary>
        internal ITriggerInteractionReader InteractionSurface
        {
            get { return interactionSurface; }
        }

        /// <summary>
        /// 对内统一返回 Trigger 正式交互语义解释器。
        /// </summary>
        private TriggerInteractionInterpreter InteractionInterpreter
        {
            get { return interactionInterpreter; }
        }

        /// <summary>
        /// 对内暴露当前 TriggerBody 的 Verb 宿主管理器。
        /// </summary>
        internal TriggerBodyVerbHostManager VerbHostManager
        {
            get { return verbHostManager; }
        }

        /// <summary>
        /// 对内暴露当前 TriggerBody 的运行时发布协调器。
        /// </summary>
        internal TriggerRuntimeCoordinator RuntimeCoordinator
        {
            get { return runtimeCoordinator; }
        }

        /// <summary>
        /// 对内暴露当前 `Trigger` owner 的运行时服务根。
        /// </summary>
        internal TriggerRuntimeServices RuntimeServices
        {
            get { return runtimeServices; }
        }

        /// <summary>
        /// 对内暴露当前已发布的战斗投影。
        /// 这里只做纯读，不在读取口补做重算。
        /// </summary>
        internal TriggerCombatProjectionState PublishedCombatProjection
        {
            get { return runtimeCoordinator != null ? runtimeCoordinator.CurrentCombatProjection : null; }
        }

        /// <summary>
        /// 对内暴露当前已发布的表现投影。
        /// 这里只做纯读，不在读取口补做重算。
        /// </summary>
        internal TriggerPresentationState PublishedPresentationProjection
        {
            get { return runtimeCoordinator != null ? runtimeCoordinator.CurrentPresentationProjection : null; }
        }

        /// <summary>
        /// 对内暴露当前已发布的视觉运行时动态状态。
        /// 这里只做纯读，不在读取口补做任何执行态推导。
        /// </summary>
        internal TriggerVisualRuntimeState PublishedVisualRuntimeState
        {
            get
            {
                return runtimeServices != null && runtimeServices.TriggerVisualRuntimeStateOwner != null
                    ? runtimeServices.TriggerVisualRuntimeStateOwner.PublishedState
                    : null;
            }
        }

        /// <summary>
        /// 从当前 owner 内部真值抓取一份正式投影构建输入。
        /// 这条路径只服务运行时发布，不通过公共 reader 反向读取自己。
        /// </summary>
        internal TriggerProjectionBuildInput BuildProjectionBuildInput()
        {
            EnsureInternalState();
            return new TriggerProjectionBuildInput
            {
                MainSlots = SnapshotSlotsForProjectionBuild(TriggerSide.Main),
                SubSlots = SnapshotSlotsForProjectionBuild(TriggerSide.Sub),
                SpecialSlots = SnapshotSlotsForProjectionBuild(TriggerSide.Special),
                MainSwitchContext = CloneSwitchContextForProjectionBuild(mainSwitchContext),
                SubSwitchContext = CloneSwitchContextForProjectionBuild(subSwitchContext),
                SpecialSwitchContext = CloneSwitchContextForProjectionBuild(specialSwitchContext),
                IsMainContainerConsistent = IsContainerConsistentForProjectionBuild(TriggerSide.Main),
                IsSubContainerConsistent = IsContainerConsistentForProjectionBuild(TriggerSide.Sub),
                IsSpecialContainerConsistent = IsContainerConsistentForProjectionBuild(TriggerSide.Special)
            };
        }

        /// <summary>
        /// 读取芯片容器当前是否持有内容。
        /// 这个判断只服务运行时发布诊断，不承担业务读取语义。
        /// </summary>
        internal bool HasHeldChipsInFormalContainer
        {
            get { return chipContainer != null && chipContainer.Count > 0; }
        }

        /// <summary>
        /// 当前是否仍有一轮待读档后完成的正式投影发布。
        /// 这条只读口只服务恢复期守卫，不承担常规业务语义。
        /// </summary>
        internal bool HasPendingPostLoadProjectionRefresh
        {
            get { return pendingPostLoadProjectionRefresh; }
        }

        /// <summary>
        /// 由当前主武器唯一运行时 owner 推进一次 Trigger 运行时。
        /// 这里只做入口收口，具体顺序统一交给 TriggerRuntimeCoordinator。
        /// </summary>
        internal bool RuntimeTick()
        {
            EnsureInternalState();
            return runtimeCoordinator == null || runtimeCoordinator.RuntimeTick();
        }

        /// <summary>
        /// 在身体约束事实变化后，立即把禁用状态应用到当前 Trigger 正式真值。
        /// 它只负责落地身体约束，不把这类离散事实再挂回 runtime tick。
        /// </summary>
        internal bool ApplyBodyConstraintChangeImmediately()
        {
            EnsureInternalState();
            if (OwnerPawn == null)
            {
                return false;
            }

            if (isRestoringPostLoad)
            {
                pendingPostLoadProjectionRefresh = true;
                return false;
            }

            if (!ForceSyncDisabledStateFromOwnerPawn())
            {
                return false;
            }

            MarkCombatProjectionDirty(ProjectionDirtyReason.DisableStateChanged);
            return runtimeCoordinator == null || runtimeCoordinator.RebuildAndPublish();
        }

        /// <summary>
        /// 对外统一返回 Trigger 正式请求口。
        /// </summary>
        internal ITriggerLoadoutCommands LoadoutCommandSurface
        {
            get { return loadoutCommandSurface; }
        }

        /// <summary>
        /// 对外统一返回 Trigger 正式事件口。
        /// </summary>
        internal ITriggerEvents EventSurface
        {
            get { return eventSurface; }
        }

        /// <summary>
        /// 便捷读取 Trigger 配置。
        /// </summary>
        private CompProperties_TriggerBody Props
        {
            get { return (CompProperties_TriggerBody)props; }
        }

        /// <summary>
        /// 当前触发体的芯片配置控制模式。
        /// </summary>
        internal TriggerLoadoutControlMode LoadoutControlMode
        {
            get
            {
                return Props != null
                    ? Props.loadoutControlMode
                    : TriggerLoadoutControlMode.PlayerConfigurable;
            }
        }

        /// <summary>
        /// 判断当前触发体是否允许玩家通过正式装配命令修改芯片。
        /// </summary>
        private bool AllowsPlayerLoadoutConfiguration
        {
            get { return LoadoutControlMode == TriggerLoadoutControlMode.PlayerConfigurable; }
        }

        /// <summary>
        /// 当前是否处于 PostLoad 恢复阶段。
        /// 这是一条生命周期护栏，不是新的业务真值。
        /// </summary>
        private bool IsRestoringPostLoad
        {
            get { return isRestoringPostLoad; }
        }

        /// <summary>
        /// 进入 PostLoad 恢复阶段。
        /// 从这一刻起，读路径不得再把 slot 真值交给运行时重算改写。
        /// </summary>
        private void BeginPostLoadRestorePhase()
        {
            isRestoringPostLoad = true;
        }

        /// <summary>
        /// 结束 PostLoad 恢复阶段。
        /// 退出后恢复常规运行时读路径。
        /// </summary>
        private void EndPostLoadRestorePhase()
        {
            isRestoringPostLoad = false;
        }

        /// <summary>
        /// 把当前已发布战斗投影标记为 dirty。
        /// 这里只记录失效来源，不直接定义发布内容。
        /// </summary>
        private void MarkCombatProjectionDirty(ProjectionDirtyReason reason)
        {
            EnsureInternalState();
            runtimeCoordinator?.MarkDirty(reason);
        }

        /// <summary>
        /// 按当前 owner 真值立即尝试发布一轮战斗投影。
        /// 成功与否只反映本轮是否完成发布，不附带攻击执行语义。
        /// </summary>
        private bool PublishCombatProjection(ProjectionDirtyReason reason)
        {
            MarkCombatProjectionDirty(reason);
            return runtimeCoordinator == null || runtimeCoordinator.RebuildAndPublish();
        }

        /// <summary>
        /// 计算当前 Trigger 已装芯片对应的预占用 Trion 总量。
        /// 这里按绑定根槽去重，避免双持镜像重复收费。
        /// </summary>
        private float CalculateReservedTrionCost()
        {
            return runtimeServices.TriggerTrionBindingService.CalculateReservedTrionCost(
                EnumerateRawSlots(),
                triggerService);
        }

        /// <summary>
        /// 把当前 Trigger 的预占用 Trion 同步到宿主 Pawn。
        /// </summary>
        private void SyncReservedTrion()
        {
            SyncReservedTrion(OwnerPawn, CalculateReservedTrionCost());
        }

        /// <summary>
        /// 把指定 Pawn 的 Trigger 预占用 Trion 同步成给定值。
        /// </summary>
        private void SyncReservedTrion(Pawn pawn, float reservedTrion)
        {
            runtimeServices.TriggerTrionBindingService.SyncReservedTrion(pawn, reservedTrion);
        }

        /// <summary>
        /// 向指定槽位装入芯片。
        /// 成功后只标记 dirty 并发布正式战斗投影，不直接触发攻击执行。
        /// </summary>
        internal bool TryLoadChip(TriggerSide side, int slotIndex, Thing chip)
        {
            if (!AllowsPlayerLoadoutConfiguration)
            {
                BdpDiagnostics.Throttled(
                    "trigger.load.reject.player_non_configurable",
                    "Load rejected: trigger body loadout is player-non-configurable.",
                    30);
                return false;
            }

            PrepareLoadoutCommandState();
            EnsureChipContainer();
            bool success = TriggerLoadoutService.TryLoadChip(BuildLoadoutContext(), side, slotIndex, chip);
            if (success)
            {
                SyncReservedTrion();
                PublishCombatProjection(ProjectionDirtyReason.LoadoutChanged);
            }

            return success;
        }

        /// <summary>
        /// 从指定槽位卸下芯片。
        /// 成功后只标记 dirty 并发布正式战斗投影，不直接触发攻击执行。
        /// </summary>
        internal bool TryUnloadChip(TriggerSide side, int slotIndex)
        {
            if (!AllowsPlayerLoadoutConfiguration)
            {
                BdpDiagnostics.Throttled(
                    "trigger.unload.reject.player_non_configurable",
                    "Unload rejected: trigger body loadout is player-non-configurable.",
                    30);
                return false;
            }

            PrepareLoadoutCommandState();
            EnsureChipContainer();
            bool success = TriggerLoadoutService.TryUnloadChip(BuildLoadoutContext(), side, slotIndex);
            if (success)
            {
                SyncReservedTrion();
                PublishCombatProjection(ProjectionDirtyReason.LoadoutChanged);
            }

            return success;
        }

        /// <summary>
        /// 销毁指定槽位中与目标 ThingID 匹配的已装载芯片。
        /// 这条命令只服务正式的一次性芯片消费，不让外部直接碰 owner 内部槽位真值。
        /// </summary>
        internal bool TryDestroyLoadedChip(TriggerSide side, int slotIndex, string expectedThingId)
        {
            PrepareLoadoutCommandState();
            EnsureChipContainer();
            bool success = TriggerLoadoutService.TryDestroyLoadedChip(BuildLoadoutContext(), side, slotIndex, expectedThingId);
            if (success)
            {
                SyncReservedTrion();
                PublishCombatProjection(ProjectionDirtyReason.LoadoutChanged);
            }

            return success;
        }

        /// <summary>
        /// 提交单侧开战体启用请求。
        /// 这里只处理 Trigger 启停真值，不承担正式攻击起手。
        /// 投影刷新延后到“正式提交激活”通知，避免切换中间态提前重建宿主。
        /// </summary>
        internal bool RequestActivate(TriggerSide side, int slotIndex)
        {
            if (CombatBodySurfaceAccess.ResolveReader(OwnerPawn)?.Phase != CombatBodyPhase.Active)
            {
                return false;
            }

            PrepareCommandState();
            return triggerService.RequestActivate(BuildSwitchContext(), side, slotIndex);
        }

        /// <summary>
        /// 提交单侧开战体停用请求。
        /// 这里只处理 Trigger 启停真值，不承担正式攻击收尾。
        /// 投影刷新延后到“正式停用”通知，避免停用延迟阶段先把宿主表切空。
        /// </summary>
        internal bool RequestDeactivate(TriggerSide side)
        {
            PrepareCommandState();
            return triggerService.RequestDeactivate(BuildSwitchContext(), side);
        }

        /// <summary>
        /// 请求把一枚正式启用的多形态芯片切到指定形态。
        /// 形态真值与投影发布作为一个原子动作处理，发布失败时保留旧形态。
        /// </summary>
        internal bool RequestSwitchChipMode(Thing chip, string targetModeKey)
        {
            PrepareCommandState();
            TriggerSlotState rootSlot = FindActiveRootSlotForChip(chip);
            if (rootSlot == null || !TriggerChipModeService.IsModeKeyValid(chip, targetModeKey))
            {
                return false;
            }

            string previousModeKey = rootSlot.CurrentModeKey;
            Exception publishException = null;
            bool switched = TriggerChipModeService.TrySwitchActiveRootMode(
                rootSlot,
                chip,
                targetModeKey,
                () => PublishCombatProjection(ProjectionDirtyReason.ChipModeChanged),
                ex => publishException = ex);
            if (switched)
            {
                return true;
            }

            // 服务已经恢复旧形态；重新标脏，确保下一次成功发布仍读取旧真值。
            MarkCombatProjectionDirty(ProjectionDirtyReason.ChipModeChanged);
            ReportChipModeSwitchFailure(chip, previousModeKey, targetModeKey, publishException);
            return false;
        }

        /// <summary>
        /// 请求把一枚正式启用的多形态芯片切到作者顺序中的下一形态。
        /// </summary>
        internal bool RequestCycleChipMode(Thing chip)
        {
            PrepareCommandState();
            TriggerSlotState rootSlot = FindActiveRootSlotForChip(chip);
            if (rootSlot == null)
            {
                return false;
            }

            string previousModeKey = rootSlot.CurrentModeKey;
            Exception publishException = null;
            bool switched = TriggerChipModeService.TryCycleActiveRootMode(
                rootSlot,
                chip,
                () => PublishCombatProjection(ProjectionDirtyReason.ChipModeChanged),
                ex => publishException = ex);
            if (switched)
            {
                return true;
            }

            MarkCombatProjectionDirty(ProjectionDirtyReason.ChipModeChanged);
            ReportChipModeSwitchFailure(chip, previousModeKey, "next", publishException);
            return false;
        }

        /// <summary>
        /// 输出一次受节流保护的形态切换失败诊断，并向玩家说明旧形态仍被保留。
        /// </summary>
        private void ReportChipModeSwitchFailure(
            Thing chip,
            string previousModeKey,
            string targetModeKey,
            Exception exception)
        {
            string chipThingId = SafeThingId(chip);
            BdpDiagnostics.Throttled(
                "trigger.chip_mode_switch_failed." + chipThingId + "." + targetModeKey,
                "芯片形态切换失败，已恢复旧形态。trigger=" + SafeThingId(parent)
                + ", pawn=" + SafeThingId(OwnerPawn)
                + ", chip=" + chipThingId
                + ", oldMode=" + (previousModeKey ?? "null")
                + ", targetMode=" + (targetModeKey ?? "null")
                + ", exception=" + (exception != null ? exception.ToString() : "none"));
            Messages.Message(
                "BDP_Message_CombatBody_TriggerSwitchFailed".Translate(),
                MessageTypeDefOf.RejectInput,
                false);
        }

        /// <summary>
        /// 装备到 Pawn 身上时同步当前预占用 Trion。
        /// </summary>
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            SyncReservedTrion();
            ApplyBodyConstraintChangeImmediately();
        }

        /// <summary>
        /// 装备解除时显式刷新这把 Trigger 对应的外围投影。
        /// 这里只清空已发布投影和 formal host，不处理攻击运行时结束判定。
        /// </summary>
        public override void Notify_Unequipped(Pawn pawn)
        {
            if (CombatBodySurfaceAccess.ResolveReader(pawn)?.Phase == CombatBodyPhase.Active)
            {
                CombatBodySurfaceAccess.ResolveCommands(pawn)?.RequestRelease();
            }

            base.Notify_Unequipped(pawn);
            SyncReservedTrion(pawn, 0f);
            ForceTeardownOnDetach(pawn);
        }

        /// <summary>
        /// 在触发体物品信息中显示其正式类别。
        /// 类别文字和说明均读取类别Def，不在运行代码中硬编码具体类别名称。
        /// </summary>
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            IEnumerable<StatDrawEntry> baseStats = base.SpecialDisplayStats();
            if (baseStats != null)
            {
                foreach (StatDrawEntry entry in baseStats)
                {
                    yield return entry;
                }
            }

            TriggerCategoryDef category = Props != null ? Props.triggerCategory : null;
            if (category == null)
            {
                yield break;
            }

            yield return new StatDrawEntry(
                StatCategoryDefOf.Weapon,
                "BDP_Stat_TriggerCategoryLabel".Translate(),
                category.LabelCap,
                category.description ?? "BDP_Stat_TriggerCategoryDescription".Translate(),
                1000);
        }
    }
}
