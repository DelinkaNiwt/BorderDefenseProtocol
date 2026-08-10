using BDP.Core.AttackExecution;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using BDP.Core.Trigger.Runtime;
using RimWorld;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 生命周期侧逻辑。
    /// 这里只保留 owner 真值恢复与显式投影刷新，不再承担 internal formal host 的持续 tick。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
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
            base.PostPostMake();
            TryInstallInitialFixedLoadout();
        }

        /// <summary>
        /// 生成或读档挂回世界时补齐 Trigger 运行态。
        /// 如果当前已经挂在 Pawn 装备位下，需要立刻把预占用 Trion 回写给宿主。
        /// </summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureInternalState();
            EnsureChipContainer();
            EnsureSlots();

            if (OwnerPawn != null)
            {
                SyncReservedTrion();
                ApplyBodyConstraintChangeImmediately();
            }
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
            foreach (Gizmo gizmo in base.CompGetEquippedGizmosExtra())
            {
                yield return gizmo;
            }

            // ★ 自由开火按钮：触发体有活跃远程芯片时补充。
            // 原版 FireAtWill 依赖 def.IsRangedWeapon，触发体 def 无 <verbs> 不满足。
            // 此处直接操作 pawn.drafter.FireAtWill，与原版行为完全一致。
            if (OwnerPawn?.drafter != null && OwnerPawn.drafter.Drafted && HasActiveRangedChip())
            {
                yield return new Command_Toggle
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
            }

            foreach (Gizmo gizmo in TriggerEquippedGizmoService.BuildEquippedGizmos(LoadoutReaderSurface, LoadoutCommandSurface, OwnerPawn))
            {
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
