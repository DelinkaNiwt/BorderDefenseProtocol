using BDP.Core.AttackExecution;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using BDP.Core.Trigger.Runtime;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 生命周期侧逻辑。
    /// 这里只保留 owner 真值恢复与显式投影刷新，不再承担 internal formal host 的持续 tick。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 最近一次已知的装备 Pawn；用于物品先脱离容器后才进入销毁回调的边界情况。
        /// </summary>
        private Pawn lastKnownOwnerPawn;

        /// <summary>
        /// 当前装备这把武器的 Pawn。
        /// </summary>
        internal Pawn OwnerPawn
        {
            get
            {
                if (parent?.ParentHolder is Pawn_EquipmentTracker equipmentTracker)
                {
                    return equipmentTracker.pawn;
                }

                return parent?.ParentHolder?.ParentHolder as Pawn;
            }
        }

        /// <summary>
        /// 当前触发体所属的中性 TriggerCategoryDef；具体业务通过该读取口判断类别。
        /// </summary>
        public TriggerCategoryDef TriggerCategory
        {
            get { return Props != null ? Props.triggerCategory : null; }
        }

        /// <summary>
        /// 判断当前 Trigger 是否仍是宿主 Pawn 的主武器 runtime owner。
        /// 只有当前主武器上的 Trigger 才允许推进正式运行时。
        /// </summary>
        internal bool IsCurrentPrimaryRuntimeOwner()
        {
            Pawn ownerPawn = OwnerPawn;
            ThingWithComps primaryEquipment = ownerPawn?.equipment?.Primary;
            return ownerPawn != null && primaryEquipment == parent;
        }

        /// <summary>
        /// 初始化内部状态、容器和槽位。
        /// </summary>
        public override void Initialize(CompProperties properties)
        {
            base.Initialize(properties);
            EnsureInternalState();
            EnsureChipContainer();
            EnsureSlots();
        }

        /// <summary>
        /// 在物品首次制造完成后应用定义中的初始固定芯片。
        /// PostLoad路径不调用此入口，因此读档不会重复预装。
        /// </summary>
        public override void PostPostMake()
        {
            long lifecycleStart = BdpDiagnostics.BeginTiming();
            base.PostPostMake();
            TraceLifecycle("post_make_begin", "respawningAfterLoad=false");
            long fixedLoadoutStart = BdpDiagnostics.BeginTiming();
            TryInstallInitialFixedLoadout();
            BdpDiagnostics.TraceTiming(
                "触发体生成 | stage=fixed_loadout",
                fixedLoadoutStart);
            TraceSlotBoundary("post_make_complete", "fixedLoadoutApplied=true");
            BdpDiagnostics.TraceTiming(
                "触发体生成 | stage=post_make_total | trigger=" + SafeThingId(parent),
                lifecycleStart);
        }

        /// <summary>
        /// 生成或读档挂回世界时补齐 Trigger 运行态。
        /// 如果当前已经挂在 Pawn 装备位下，需要立刻把预占用 Trion 回写给宿主。
        /// </summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            long lifecycleStart = BdpDiagnostics.BeginTiming();
            base.PostSpawnSetup(respawningAfterLoad);
            long ensureStateStart = BdpDiagnostics.BeginTiming();
            EnsureInternalState();
            EnsureChipContainer();
            EnsureSlots();
            BdpDiagnostics.TraceTiming(
                "触发体生成 | stage=post_spawn_state_ready | respawningAfterLoad=" + respawningAfterLoad,
                ensureStateStart);

            TraceLifecycle(
                "post_spawn_setup",
                "respawningAfterLoad=" + respawningAfterLoad
                + " | owner=" + SafeThingId(OwnerPawn));

            if (OwnerPawn != null)
            {
                lastKnownOwnerPawn = OwnerPawn;
                long trionBindingStart = BdpDiagnostics.BeginTiming();
                TriggerTrionCapacityBindingRegistry.NotifyEquipped(OwnerPawn, this);
                SyncReservedTrion();
                BdpDiagnostics.TraceTiming(
                    "触发体生成 | stage=post_spawn_trion_binding | pawn=" + SafeThingId(OwnerPawn),
                    trionBindingStart);

                long bodyConstraintStart = BdpDiagnostics.BeginTiming();
                ApplyBodyConstraintChangeImmediately();
                BdpDiagnostics.TraceTiming(
                    "触发体生成 | stage=post_spawn_body_constraint | pawn=" + SafeThingId(OwnerPawn),
                    bodyConstraintStart);
            }

            BdpDiagnostics.TraceTiming(
                "触发体生成 | stage=post_spawn_total | trigger=" + SafeThingId(parent),
                lifecycleStart);
        }

        /// <summary>
        /// 触发体被摧毁时通知容量绑定器清理外部合并池。
        /// </summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Pawn ownerPawn = OwnerPawn ?? lastKnownOwnerPawn;
            TriggerTrionCapacityBindingRegistry.NotifyDestroyed(ownerPawn, this);
            base.PostDestroy(mode, previousMap);
        }

        /// <summary>
        /// 存档读写 Trigger 真值，并在读档后恢复完整性。
        /// 读档后允许做一次显式投影刷新，但不在这里重建额外运行时体系。
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            EnsureInternalState();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                EnsureChipContainer();
                EnsureSlots();
            }

            Scribe_Collections.Look(ref mainSlots, "mainSlots", LookMode.Deep);
            Scribe_Collections.Look(ref subSlots, "subSlots", LookMode.Deep);
            Scribe_Collections.Look(ref specialSlots, "specialSlots", LookMode.Deep);
            Scribe_Deep.Look(ref chipContainer, "chipContainer", this);
            Scribe_Deep.Look(ref mainSwitchContext, "mainSwitchContext");
            Scribe_Deep.Look(ref subSwitchContext, "subSwitchContext");
            Scribe_Deep.Look(ref specialSwitchContext, "specialSwitchContext");
            Scribe_Values.Look(ref combatBodyUnavailableDisable, "combatBodyUnavailableDisable", false);
            verbHostManager?.ExposeVerbShells();

            // 原版 VerbTracker 会在 ResolvingCrossRefs 阶段把 loaded verb 重新接回 owner。
            // formal host 壳如果晚到 PostLoadInit 才补表面，原版 stance/job 可能已经先把它判成 bugged verb。
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                EnsureInternalState();
                EnsureChipContainer();
                EnsureSlots();
                verbHostManager?.RestoreShellsPostLoad();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                BeginPostLoadRestorePhase();
                try
                {
                    EnsureInternalState();
                    EnsureChipContainer();
                    EnsureSlots();
                    bool slotTruthReady = RestoreSlotTruth();
                    RebuildContainerFromSlotTruth();
                    verbHostManager?.RestoreShellsPostLoad();
                    pendingPostLoadProjectionRefresh = true;
                    pendingPostLoadProjectionRefresh = !slotTruthReady || !TryFinalizePostLoadProjectionRefresh();
                }
                finally
                {
                    EndPostLoadRestorePhase();
                }
            }
        }

        /// <summary>
        /// 返回 Trigger 直接持有的东西容器。
        /// </summary>
        public ThingOwner GetDirectlyHeldThings()
        {
            EnsureChipContainer();
            return chipContainer;
        }

        /// <summary>
        /// 把直接持有的子容器追加给 RimWorld。
        /// </summary>
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        /// <summary>
        /// 生成装备状态下显示的操作按钮。
        /// </summary>
        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
        {
            // 原版在筛选装备攻击按钮之前就枚举组件额外按钮，组件必须自行校验控制资格。
            // 这里只约束玩家操作界面，敌方自动攻击与触发体运行继续使用原有入口。
            if (OwnerPawn == null || !OwnerPawn.IsPlayerControlled)
            {
                yield break;
            }

            AcceptanceReport allowsDrafting = OwnerPawn.GetLord()?.AllowsDrafting(OwnerPawn) ?? (AcceptanceReport)true;

            foreach (Gizmo gizmo in base.CompGetEquippedGizmosExtra())
            {
                yield return gizmo;
            }

            // ★ 自由开火按钮：触发体有活跃远程芯片时补充。
            // 原版 FireAtWill 依赖 def.IsRangedWeapon，触发体 def 无 <verbs> 不满足。
            // 此处直接操作 pawn.drafter.FireAtWill，与原版行为完全一致。
            if (OwnerPawn?.drafter != null && OwnerPawn.drafter.Drafted && HasActiveRangedChip())
            {
                var fireAtWillCommand = new Command_Toggle
                {
                    hotKey = KeyBindingDefOf.Misc6,
                    isActive = () => OwnerPawn?.drafter?.FireAtWill ?? false,
                    toggleAction = () =>
                    {
                        if (OwnerPawn?.drafter != null)
                            OwnerPawn.drafter.FireAtWill = !OwnerPawn.drafter.FireAtWill;
                    },
                    icon = TexCommand.FireAtWill,
                    defaultLabel = "CommandFireAtWillLabel".Translate(),
                    defaultDesc = "CommandFireAtWillDesc".Translate(),
                    tutorTag = "FireAtWillToggle"
                };
                if (!allowsDrafting)
                {
                    fireAtWillCommand.Disable(allowsDrafting.Reason);
                }
                yield return fireAtWillCommand;
            }

            foreach (Gizmo gizmo in TriggerEquippedGizmoService.BuildEquippedGizmos(LoadoutReaderSurface, LoadoutCommandSurface, OwnerPawn))
            {
                // 与原版攻击命令一致：任务禁止征召时保留按钮并展示原版禁用原因。
                if (!allowsDrafting && gizmo is Command command && !command.Disabled)
                {
                    command.Disable(allowsDrafting.Reason);
                }
                yield return gizmo;
            }
        }

        /// <summary>
        /// 在 Trigger 真值变化后显式刷新外围投影与宿主状态。
        /// 这里只负责 post-load 的恢复前置检查，真正发布委托给运行时协调器。
        /// </summary>
        internal bool TryFinalizePostLoadProjectionRefresh()
        {
            if (!pendingPostLoadProjectionRefresh)
            {
                return true;
            }

            if (OwnerPawn == null)
            {
                return false;
            }

            if (!RestoreSlotTruth())
            {
                return false;
            }

            RebuildContainerFromSlotTruth();
            ForceSyncDisabledStateFromOwnerPawn();
            MarkCombatProjectionDirty(ProjectionDirtyReason.PostLoadFinalize);
            if (runtimeCoordinator == null || !runtimeCoordinator.TryFinalizePostLoadProjectionRefresh())
            {
                return false;
            }

            SyncReservedTrion();
            pendingPostLoadProjectionRefresh = false;
            return true;
        }

        /// <summary>
        /// 安全读取 ThingID。
        /// </summary>
        private static string SafeThingId(Thing thing)
        {
            return thing != null && !string.IsNullOrWhiteSpace(thing.ThingID)
                ? thing.ThingID
                : "null";
        }
    }
}
