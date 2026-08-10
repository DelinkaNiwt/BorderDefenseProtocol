using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Expressions.Runtime;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Projection;
using BDP.Core.Trigger.Runtime;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达系统正式服务。
    /// 它统一承接表达读取、快照装配、结果命中和宿主投影同步，不再把这些能力拆散到外围门层。
    /// </summary>
    internal sealed class ExpressionService : IExpressionReader
    {
        /// <summary>
        /// 当前服务绑定的正式说明投影器。
        /// </summary>
        private readonly DefaultExpressionInfoProjector infoProjector;

        /// <summary>
        /// 当前服务绑定的正式手动入口投影器。
        /// </summary>
        private readonly DefaultManualEntryProjector manualProjector;

        /// <summary>
        /// 当前服务绑定的正式视觉投影器。
        /// </summary>
        private readonly DefaultVisualProjectionBuilder visualProjector;

        /// <summary>
        /// 当前服务绑定的默认主表达选择器。
        /// </summary>
        private readonly DefaultPrimaryExpressionSelector primarySelector;

        /// <summary>
        /// 当前服务绑定的默认宿主总同步器。
        /// </summary>
        private readonly DefaultExpressionHostSynchronizer hostSynchronizer;

        /// <summary>
        /// 当前服务绑定的表达持续 Trion 账本对账器。
        /// </summary>
        private readonly ExpressionSustainDrainService sustainDrainService;

        /// <summary>
        /// 当前服务绑定的共享表达运行时仓库。
        /// </summary>
        private readonly ExpressionRuntimeRepository runtimeRepository;

        /// <summary>
        /// 初始化正式服务和长期复用的投影/同步依赖。
        /// </summary>
        public ExpressionService(ExpressionRuntimeRepository runtimeRepository)
        {
            this.runtimeRepository = runtimeRepository ?? new ExpressionRuntimeRepository();
            infoProjector = new DefaultExpressionInfoProjector();
            manualProjector = new DefaultManualEntryProjector();
            visualProjector = new DefaultVisualProjectionBuilder();
            primarySelector = new DefaultPrimaryExpressionSelector();
            hostSynchronizer = new DefaultExpressionHostSynchronizer(
                new DefaultExpressionAbilityHostSynchronizer(),
                new DefaultExpressionHediffHostSynchronizer());
            sustainDrainService = new ExpressionSustainDrainService();
        }

        /// <summary>
        /// 读取指定 Pawn 当前已发布的战斗投影。
        /// 普通读取只消费已发布结果，不触发运行时协调。
        /// </summary>
        public TriggerCombatProjectionState GetCombatProjection(Pawn pawn)
        {
            return TryGetPublishedCombatProjection(pawn, out TriggerCombatProjectionState projection)
                ? projection ?? TriggerCombatProjectionState.CreateEmpty(0)
                : TriggerCombatProjectionState.CreateEmpty(0);
        }

        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部正式表达结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> GetExpressionResults(Pawn pawn)
        {
            return GetPublishedChannelIndex(pawn).AllResults;
        }

        /// <summary>
        /// 按类别读取指定 Pawn 当前已发布的正式表达结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> GetExpressionResults(Pawn pawn, ExpressionResultKind kind)
        {
            switch (kind)
            {
                case ExpressionResultKind.Verb:
                    return GetVerbResults(pawn);
                case ExpressionResultKind.Ability:
                    return GetAbilityResults(pawn);
                case ExpressionResultKind.Hediff:
                    return GetHediffResults(pawn);
                case ExpressionResultKind.Passive:
                    return GetPassiveResults(pawn);
                default:
                    return new List<FormalExpressionResult>();
            }
        }

        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部 Verb 结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> GetVerbResults(Pawn pawn)
        {
            return GetPublishedChannelIndex(pawn).VerbResults;
        }

        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部 Ability 结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> GetAbilityResults(Pawn pawn)
        {
            return GetPublishedChannelIndex(pawn).AbilityResults;
        }

        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部 Hediff 结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> GetHediffResults(Pawn pawn)
        {
            return GetPublishedChannelIndex(pawn).HediffResults;
        }

        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部 Passive 结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> GetPassiveResults(Pawn pawn)
        {
            return GetPublishedChannelIndex(pawn).PassiveResults;
        }

        /// <summary>
        /// 按被动键读取指定 Pawn 当前已发布的 Passive 结果。
        /// </summary>
        public IReadOnlyList<FormalExpressionResult> GetPassiveResults(Pawn pawn, string passiveKey)
        {
            if (string.IsNullOrWhiteSpace(passiveKey))
            {
                return new List<FormalExpressionResult>();
            }

            ExpressionChannelIndex channelIndex = GetPublishedChannelIndex(pawn);
            if (channelIndex.PassiveResultsByKey == null)
            {
                return new List<FormalExpressionResult>();
            }

            IReadOnlyList<FormalExpressionResult> results;
            return channelIndex.PassiveResultsByKey.TryGetValue(passiveKey, out results)
                ? results ?? new List<FormalExpressionResult>()
                : new List<FormalExpressionResult>();
        }

        /// <summary>
        /// 判断指定 Pawn 当前是否存在可用的目标 PassiveKey。
        /// </summary>
        public bool HasPassiveKey(Pawn pawn, string passiveKey)
        {
            return TryGetPassive(pawn, passiveKey, out FormalExpressionResult _);
        }

        /// <summary>
        /// 尝试读取指定 Pawn 当前第一条可用的目标 Passive 结果。
        /// </summary>
        public bool TryGetPassive(Pawn pawn, string passiveKey, out FormalExpressionResult result)
        {
            result = null;
            IReadOnlyList<FormalExpressionResult> passiveResults = GetPassiveResults(pawn, passiveKey);
            if (passiveResults == null)
            {
                return false;
            }

            for (int i = 0; i < passiveResults.Count; i++)
            {
                FormalExpressionResult candidate = passiveResults[i];
                if (candidate == null || !candidate.IsAvailable)
                {
                    continue;
                }

                result = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 读取指定 Pawn 当前说明投影结果。
        /// 常规读取默认不附带诊断，避免把定义校验和契约解释压进普通 UI 热路径。
        /// </summary>
        public ExpressionInfoProjection GetInfoProjection(Pawn pawn, bool includeDiagnostics = false)
        {
            ExpressionInfoProjection projection =
                TryGetPublishedPresentationProjection(pawn, out TriggerPresentationState presentationState)
                    ? presentationState.InfoProjection ?? CreateEmptyInfoProjection()
                    : CreateEmptyInfoProjection();
            if (includeDiagnostics)
            {
                projection = CloneInfoProjection(projection);
                AttachContractDiagnostics(pawn, projection);
            }

            return projection;
        }

        /// <summary>
        /// 读取指定 Pawn 当前手动入口投影结果。
        /// </summary>
        public ManualEntryProjection GetManualProjection(Pawn pawn)
        {
            return TryGetPublishedPresentationProjection(pawn, out TriggerPresentationState presentationState)
                ? presentationState.ManualProjection ?? CreateEmptyManualProjection()
                : CreateEmptyManualProjection();
        }

        /// <summary>
        /// 读取指定 Pawn 当前视觉投影结果。
        /// </summary>
        public VisualExpressionProjection GetVisualProjection(Pawn pawn)
        {
            return TryGetPublishedPresentationProjection(pawn, out TriggerPresentationState presentationState)
                ? presentationState.VisualProjection ?? CreateEmptyVisualProjection()
                : CreateEmptyVisualProjection();
        }

        /// <summary>
        /// 从一份已选表达快照构建说明投影。
        /// 这条口只给运行时发布 owner 使用，不作为普通 UI 读路径。
        /// </summary>
        internal ExpressionInfoProjection BuildPublishedInfoProjection(ExpressionSnapshot snapshot)
        {
            EnsurePublicationSnapshot(snapshot);
            return infoProjector.Build(snapshot);
        }

        /// <summary>
        /// 从一份已选表达快照构建手动入口投影。
        /// 这条口只给运行时发布 owner 使用，不作为普通 UI 读路径。
        /// </summary>
        internal ManualEntryProjection BuildPublishedManualProjection(ExpressionSnapshot snapshot)
        {
            return manualProjector.Build(snapshot);
        }

        /// <summary>
        /// 从一份已选表达快照构建视觉投影。
        /// 这条口只给运行时发布 owner 使用，不作为普通 UI 读路径。
        /// </summary>
        internal VisualExpressionProjection BuildPublishedVisualProjection(ExpressionSnapshot snapshot)
        {
            return visualProjector.Build(snapshot);
        }

        /// <summary>
        /// 用调用方已知可用的 Trigger 读取口构建正式总表。
        /// 这条路径用于 post-load 恢复阶段，避免 owner 自己又回头依赖 pawn.Primary 链去找自己。
        /// </summary>
        internal ExpressionSnapshot BuildSelectedSnapshot(Pawn pawn, ITriggerLoadoutReader triggerLoadoutReader)
        {
            if (triggerLoadoutReader == null)
            {
                return new ExpressionSnapshot();
            }

            ExpressionSnapshotBuilder snapshotBuilder = runtimeRepository.SnapshotBuilder;
            ExpressionSnapshot snapshot = snapshotBuilder.Build(pawn, triggerLoadoutReader);
            snapshot = primarySelector.Select(snapshot);
            return snapshot;
        }

        /// <summary>
        /// 用 owner 内部投影构建输入构建正式总表。
        /// 这条路径只给 TriggerRuntimeCoordinator 使用，避免 owner 为发布自己又回头走公共 reader。
        /// </summary>
        internal ExpressionSnapshot BuildSelectedSnapshot(Pawn pawn, TriggerProjectionBuildInput buildInput)
        {
            if (buildInput == null)
            {
                return new ExpressionSnapshot();
            }

            return BuildSelectedSnapshot(pawn, new TriggerProjectionBuildLoadoutReader(buildInput));
        }

        /// <summary>
        /// 按当前正式总表同步外围宿主投影。
        /// 这条路径只负责把已构建好的表达真值显式投影到宿主系统，不承担额外运行时编排。
        /// </summary>
        internal void SyncProjectedHosts(Pawn pawn, ExpressionSnapshot snapshot)
        {
            if (pawn == null || snapshot == null)
            {
                return;
            }

            EnsurePublicationSnapshot(snapshot);
            hostSynchronizer?.Sync(pawn, snapshot);
            sustainDrainService?.Reconcile(pawn, snapshot);
        }

        /// <summary>
        /// 为当前快照补齐旁路发布观察快照。
        /// 这条信息只服务说明和排查，不影响宿主同步行为。
        /// </summary>
        private void EnsurePublicationSnapshot(ExpressionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.PublicationSnapshot = hostSynchronizer?.BuildPublicationSnapshot(snapshot);
        }

        /// <summary>
        /// 读取指定 Pawn 当前已发布的战斗投影。
        /// 普通表达/UI 读取只消费已发布状态，不在这里顺手推进 Trigger runtime。
        /// </summary>
        private static bool TryGetPublishedCombatProjection(Pawn pawn, out TriggerCombatProjectionState projection)
        {
            projection = null;
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            projection = triggerBody != null ? triggerBody.PublishedCombatProjection : null;
            return projection != null;
        }

        /// <summary>
        /// 读取指定 Pawn 当前已发布的四类并联索引。
        /// 无已发布投影时返回稳定空索引。
        /// </summary>
        private static ExpressionChannelIndex GetPublishedChannelIndex(Pawn pawn)
        {
            if (!TryGetPublishedCombatProjection(pawn, out TriggerCombatProjectionState projection))
            {
                return ExpressionChannelIndex.Empty();
            }

            return projection != null && projection.ChannelIndex != null
                ? projection.ChannelIndex
                : ExpressionChannelIndex.Empty();
        }

        /// <summary>
        /// 读取指定 Pawn 当前已发布的表现投影。
        /// 普通表达/UI 读取只消费已发布状态，不在这里顺手推进 Trigger runtime。
        /// </summary>
        private static bool TryGetPublishedPresentationProjection(Pawn pawn, out TriggerPresentationState projection)
        {
            projection = null;
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            projection = triggerBody != null ? triggerBody.PublishedPresentationProjection : null;
            return projection != null;
        }

        /// <summary>
        /// 克隆一份说明投影，避免诊断读取污染已发布缓存对象。
        /// </summary>
        private static ExpressionInfoProjection CloneInfoProjection(ExpressionInfoProjection projection)
        {
            if (projection == null)
            {
                return CreateEmptyInfoProjection();
            }

            return new ExpressionInfoProjection
            {
                Lines = projection.Lines != null ? new List<string>(projection.Lines) : new List<string>(),
                Entries = projection.Entries != null
                    ? new List<ExpressionInfoProjectionEntry>(projection.Entries)
                    : new List<ExpressionInfoProjectionEntry>(),
                PrimaryRangedResultId = projection.PrimaryRangedResultId,
                PrimaryMeleeResultId = projection.PrimaryMeleeResultId,
                CurrentExecutingResultId = projection.CurrentExecutingResultId,
                HasSpecialWeaponOverride = projection.HasSpecialWeaponOverride,
                ContractDiagnostics = projection.ContractDiagnostics != null
                    ? new List<ExpressionContractDiagnosticEntry>(projection.ContractDiagnostics)
                    : new List<ExpressionContractDiagnosticEntry>(),
                ChipDefinitionDiagnostics = projection.ChipDefinitionDiagnostics != null
                    ? new List<ChipDefinitionDiagnosticEntry>(projection.ChipDefinitionDiagnostics)
                    : new List<ChipDefinitionDiagnosticEntry>()
            };
        }

        /// <summary>
        /// 把 owner 内部投影构建输入适配成表达总表构建所需的只读读取口。
        /// 这只是一次运行时发布内部桥接，不对外暴露为正式 reader。
        /// </summary>
        private sealed class TriggerProjectionBuildLoadoutReader : ITriggerLoadoutReader
        {
            /// <summary>
            /// 当前适配器绑定的 owner 内部构建输入。
            /// </summary>
            private readonly TriggerProjectionBuildInput buildInput;

            /// <summary>
            /// 用指定 owner 内部构建输入构造适配器。
            /// </summary>
            public TriggerProjectionBuildLoadoutReader(TriggerProjectionBuildInput buildInput)
            {
                this.buildInput = buildInput;
            }

            /// <summary>
            /// 投影读取适配器不承载玩家装配入口，使用可配置默认值满足只读接口契约。
            /// </summary>
            public TriggerLoadoutControlMode LoadoutControlMode
            {
                get { return TriggerLoadoutControlMode.PlayerConfigurable; }
            }

            /// <summary>
            /// 读取全部槽位快照。
            /// </summary>
            public IEnumerable<ITriggerSlotState> GetAllSlots()
            {
                foreach (TriggerSlotState slot in ResolveSlots(TriggerSide.Main))
                {
                    yield return slot;
                }

                foreach (TriggerSlotState slot in ResolveSlots(TriggerSide.Sub))
                {
                    yield return slot;
                }

                foreach (TriggerSlotState slot in ResolveSlots(TriggerSide.Special))
                {
                    yield return slot;
                }
            }

            /// <summary>
            /// 按侧读取槽位快照。
            /// </summary>
            public IEnumerable<ITriggerSlotState> GetSlots(TriggerSide side)
            {
                foreach (TriggerSlotState slot in ResolveSlots(side))
                {
                    yield return slot;
                }
            }

            /// <summary>
            /// 读取所有正式激活槽位快照。
            /// </summary>
            public IEnumerable<ITriggerSlotState> GetActiveSlots()
            {
                foreach (ITriggerSlotState slot in GetAllSlots())
                {
                    if (slot != null && slot.IsActive)
                    {
                        yield return slot;
                    }
                }
            }

            /// <summary>
            /// 读取某一侧当前正式激活槽位快照。
            /// </summary>
            public ITriggerSlotState GetActiveSlot(TriggerSide side)
            {
                foreach (TriggerSlotState slot in ResolveSlots(side))
                {
                    if (slot != null && slot.IsActive)
                    {
                        return slot;
                    }
                }

                return null;
            }

            /// <summary>
            /// 读取某一侧当前正在切换到的目标槽位快照。
            /// </summary>
            public ITriggerSlotState GetActivatingSlot(TriggerSide side)
            {
                SwitchContext context = ResolveSwitchContext(side);
                int index = context != null ? context.targetSlotIndex : -1;
                IReadOnlyList<TriggerSlotState> slots = ResolveSlots(side);
                if (index < 0 || slots == null || index >= slots.Count)
                {
                    return null;
                }

                return slots[index];
            }

            /// <summary>
            /// 读取某一侧当前局部切换状态快照。
            /// </summary>
            public ITriggerSwitchState GetSwitchState(TriggerSide side)
            {
                return TriggerSwitchStateSnapshot.FromContext(ResolveSwitchContext(side));
            }

            /// <summary>
            /// 读取某枚芯片当前正式形态键。
            /// </summary>
            public string GetChipModeKey(Thing chip)
            {
                TriggerSlotState rootSlot = FindActiveRootSlotForChip(chip);
                return rootSlot != null
                    && TriggerChipModeService.IsModeKeyValid(chip, rootSlot.CurrentModeKey)
                    ? rootSlot.CurrentModeKey
                    : null;
            }

            /// <summary>
            /// 读取某枚正式启用多形态芯片的有序形态选项。
            /// </summary>
            public IReadOnlyList<ChipModeOptionSnapshot> GetChipModeOptions(Thing chip)
            {
                TriggerSlotState rootSlot = FindActiveRootSlotForChip(chip);
                return rootSlot != null
                    && TriggerChipModeService.IsModeKeyValid(chip, rootSlot.CurrentModeKey)
                    ? TriggerChipModeService.BuildOptions(chip)
                    : System.Array.Empty<ChipModeOptionSnapshot>();
            }

            /// <summary>
            /// 在投影快照内把芯片实体归一到正式启用根槽。
            /// </summary>
            private TriggerSlotState FindActiveRootSlotForChip(Thing chip)
            {
                if (chip == null)
                {
                    return null;
                }

                foreach (ITriggerSlotState slotState in GetAllSlots())
                {
                    TriggerSlotState slot = slotState as TriggerSlotState;
                    if (slot == null || slot.LoadedChip != chip)
                    {
                        continue;
                    }

                    TriggerSlotState rootSlot = slot.IsBindingMirror
                        ? ResolveSlot(slot.BindingRootSide, slot.BindingRootIndex)
                        : slot;
                    if (rootSlot != null
                        && rootSlot.IsActive
                        && !rootSlot.IsBindingMirror
                        && rootSlot.LoadedChip == chip)
                    {
                        return rootSlot;
                    }
                }

                return null;
            }

            /// <summary>
            /// 按侧别和索引读取单个投影槽位快照。
            /// </summary>
            private TriggerSlotState ResolveSlot(TriggerSide side, int index)
            {
                IReadOnlyList<TriggerSlotState> slots = ResolveSlots(side);
                return index >= 0 && index < slots.Count ? slots[index] : null;
            }

            /// <summary>
            /// 解析指定侧对应的槽位快照集合。
            /// </summary>
            private IReadOnlyList<TriggerSlotState> ResolveSlots(TriggerSide side)
            {
                if (buildInput == null)
                {
                    return new List<TriggerSlotState>();
                }

                switch (side)
                {
                    case TriggerSide.Main:
                        return buildInput.MainSlots ?? new List<TriggerSlotState>();
                    case TriggerSide.Sub:
                        return buildInput.SubSlots ?? new List<TriggerSlotState>();
                    default:
                        return buildInput.SpecialSlots ?? new List<TriggerSlotState>();
                }
            }

            /// <summary>
            /// 解析指定侧对应的切换上下文快照。
            /// </summary>
            private SwitchContext ResolveSwitchContext(TriggerSide side)
            {
                if (buildInput == null)
                {
                    return null;
                }

                switch (side)
                {
                    case TriggerSide.Main:
                        return buildInput.MainSwitchContext;
                    case TriggerSide.Sub:
                        return buildInput.SubSwitchContext;
                    default:
                        return buildInput.SpecialSwitchContext;
                }
            }
        }

        /// <summary>
        /// 构建空说明投影。
        /// 这是普通读路径在无已发布结果时的稳定兜底对象。
        /// </summary>
        private static ExpressionInfoProjection CreateEmptyInfoProjection()
        {
            return new ExpressionInfoProjection
            {
                Lines = new List<string>(),
                Entries = new List<ExpressionInfoProjectionEntry>(),
                ContractDiagnostics = new List<ExpressionContractDiagnosticEntry>(),
                ChipDefinitionDiagnostics = new List<ChipDefinitionDiagnosticEntry>()
            };
        }

        /// <summary>
        /// 构建空手动入口投影。
        /// 这是普通读路径在无已发布结果时的稳定兜底对象。
        /// </summary>
        private static ManualEntryProjection CreateEmptyManualProjection()
        {
            return new ManualEntryProjection
            {
                Groups = new List<ManualEntryProjectionGroup>()
            };
        }

        /// <summary>
        /// 构建空视觉投影。
        /// 这是普通读路径在无已发布结果时的稳定兜底对象。
        /// </summary>
        private static VisualExpressionProjection CreateEmptyVisualProjection()
        {
            return new VisualExpressionProjection
            {
                RelationKind = VisualExpressionRelationKind.None,
                ResidentEntries = new List<VisualResidentEntry>(),
                ActiveWeaponChipInstanceCount = 0,
                HostEquipmentRenderMode = HostEquipmentRenderMode.Keep,
                ExecutionFocusPolicy = VisualExecutionFocusPolicy.None,
                MuzzleFollowPolicy = VisualMuzzleFollowPolicy.None
            };
        }

        /// <summary>
        /// 为说明投影补入当前活跃芯片契约诊断。
        /// </summary>
        private static void AttachContractDiagnostics(Pawn pawn, ExpressionInfoProjection projection)
        {
            if (projection == null)
            {
                return;
            }

            ITriggerLoadoutReader triggerLoadoutReader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            if (triggerLoadoutReader == null)
            {
                projection.ContractDiagnostics = new List<ExpressionContractDiagnosticEntry>();
                return;
            }

            ExpressionRuntimeRepository runtimeRepository = ExpressionSurfaceAccess.ResolveRuntimeRepository(pawn);
            IChipDefinitionReader chipDefinitionReader = runtimeRepository != null ? runtimeRepository.ChipDefinitionReader : null;
            IChipExpressionContractInterpreter contractInterpreter =
                runtimeRepository != null ? runtimeRepository.ContractInterpreter : null;
            if (chipDefinitionReader == null || contractInterpreter == null)
            {
                projection.ChipDefinitionDiagnostics = new List<ChipDefinitionDiagnosticEntry>();
                projection.ContractDiagnostics = new List<ExpressionContractDiagnosticEntry>();
                return;
            }
            List<ChipDefinitionDiagnosticEntry> chipDefinitionDiagnostics =
                BuildChipDefinitionDiagnostics(triggerLoadoutReader, chipDefinitionReader);
            List<ExpressionContractDiagnosticEntry> diagnostics =
                BuildContractDiagnostics(triggerLoadoutReader, chipDefinitionReader, contractInterpreter);
            projection.ChipDefinitionDiagnostics = chipDefinitionDiagnostics;
            projection.ContractDiagnostics = diagnostics;

            List<string> lines = projection.Lines != null
                ? new List<string>(projection.Lines)
                : new List<string>();
            lines.Add("芯片定义诊断：count=" + chipDefinitionDiagnostics.Count);

            for (int i = 0; i < chipDefinitionDiagnostics.Count; i++)
            {
                ChipDefinitionDiagnosticEntry diagnostic = chipDefinitionDiagnostics[i];
                if (diagnostic == null)
                {
                    continue;
                }

                lines.Add(BuildChipDefinitionDiagnosticLine(diagnostic));
            }

            lines.Add("芯片契约诊断：count=" + diagnostics.Count);

            for (int i = 0; i < diagnostics.Count; i++)
            {
                ExpressionContractDiagnosticEntry diagnostic = diagnostics[i];
                if (diagnostic == null)
                {
                    continue;
                }

                lines.Add(BuildContractDiagnosticLine(diagnostic));
            }

            projection.Lines = lines;
        }

        /// <summary>
        /// 汇总当前活跃芯片的定义层诊断。
        /// </summary>
        private static List<ChipDefinitionDiagnosticEntry> BuildChipDefinitionDiagnostics(
            ITriggerLoadoutReader triggerLoadoutReader,
            IChipDefinitionReader chipDefinitionReader)
        {
            List<ChipDefinitionDiagnosticEntry> diagnostics = new List<ChipDefinitionDiagnosticEntry>();
            if (triggerLoadoutReader == null || chipDefinitionReader == null)
            {
                return diagnostics;
            }

            IEnumerable<ITriggerSlotState> activeSlots = triggerLoadoutReader.GetActiveSlots();
            if (activeSlots == null)
            {
                return diagnostics;
            }

            HashSet<string> seenChipIds = new HashSet<string>();
            foreach (ITriggerSlotState slot in activeSlots)
            {
                if (slot == null || slot.LoadedChip == null || slot.IsBindingMirror)
                {
                    continue;
                }

                if (!seenChipIds.Add(slot.LoadedChip.ThingID))
                {
                    continue;
                }

                ChipDefinitionReadResult chipReadResult = chipDefinitionReader.Read(slot.LoadedChip);
                diagnostics.Add(new ChipDefinitionDiagnosticEntry
                {
                    ChipThingId = slot.LoadedChip.ThingID,
                    ChipLabel = slot.LoadedChip.LabelCap.ToString(),
                    IsValid = chipReadResult != null
                        && chipReadResult.Validation != null
                        && chipReadResult.Validation.IsValid,
                    Errors = TranslateValidationMessages(
                        chipReadResult != null ? chipReadResult.Validation?.Errors : null),
                    Warnings = TranslateValidationMessages(
                        chipReadResult != null ? chipReadResult.Validation?.Warnings : null)
                });
            }

            return diagnostics;
        }

        /// <summary>
        /// 汇总当前活跃芯片的契约诊断。
        /// </summary>
        private static List<ExpressionContractDiagnosticEntry> BuildContractDiagnostics(
            ITriggerLoadoutReader triggerLoadoutReader,
            IChipDefinitionReader chipDefinitionReader,
            IChipExpressionContractInterpreter contractInterpreter)
        {
            List<ExpressionContractDiagnosticEntry> diagnostics = new List<ExpressionContractDiagnosticEntry>();
            if (triggerLoadoutReader == null || chipDefinitionReader == null || contractInterpreter == null)
            {
                return diagnostics;
            }

            IEnumerable<ITriggerSlotState> activeSlots = triggerLoadoutReader.GetActiveSlots();
            if (activeSlots == null)
            {
                return diagnostics;
            }

            HashSet<string> seenChipIds = new HashSet<string>();
            foreach (ITriggerSlotState slot in activeSlots)
            {
                if (slot == null || slot.LoadedChip == null || slot.IsBindingMirror)
                {
                    continue;
                }

                if (!seenChipIds.Add(slot.LoadedChip.ThingID))
                {
                    continue;
                }

                ChipDefinitionReadResult chipReadResult = chipDefinitionReader.Read(slot.LoadedChip);
                ChipExpressionConfig config = chipReadResult != null
                    && chipReadResult.Contract != null
                    && chipReadResult.Validation != null
                    && chipReadResult.Validation.IsValid
                    && chipReadResult.Contract.Expression != null
                    && chipReadResult.Contract.Expression.HasExpressionBlock
                    ? chipReadResult.Contract.Expression.Config
                    : null;
                ChipExpressionResolvedContract resolvedContract =
                    config != null
                        ? contractInterpreter.Resolve(slot.LoadedChip, config, triggerLoadoutReader)
                        : null;
                diagnostics.Add(new ExpressionContractDiagnosticEntry
                {
                    ChipThingId = slot.LoadedChip.ThingID,
                    ChipLabel = slot.LoadedChip.LabelCap.ToString(),
                    IsValid = resolvedContract != null
                        && resolvedContract.Validation != null
                        && resolvedContract.Validation.IsValid,
                    AcceptedEntryCount = resolvedContract != null
                        && resolvedContract.Contract != null
                        && resolvedContract.Contract.Entries != null
                        ? resolvedContract.Contract.Entries.Count
                        : 0,
                    Errors = resolvedContract != null && resolvedContract.Validation != null
                        ? (IReadOnlyList<string>)resolvedContract.Validation.Errors
                        : new List<string>(),
                    Warnings = resolvedContract != null && resolvedContract.Validation != null
                        ? (IReadOnlyList<string>)resolvedContract.Validation.Warnings
                        : new List<string>()
                });
            }

            return diagnostics;
        }

        /// <summary>
        /// 构建单枚芯片的契约诊断说明行。
        /// </summary>
        private static string BuildContractDiagnosticLine(ExpressionContractDiagnosticEntry diagnostic)
        {
            string chipLabel = string.IsNullOrWhiteSpace(diagnostic.ChipLabel) ? "(未知芯片)" : diagnostic.ChipLabel;
            int errorCount = diagnostic.Errors != null ? diagnostic.Errors.Count : 0;
            int warningCount = diagnostic.Warnings != null ? diagnostic.Warnings.Count : 0;
            return chipLabel
                + " | valid=" + diagnostic.IsValid
                + " | acceptedEntries=" + diagnostic.AcceptedEntryCount
                + " | errors=" + errorCount
                + " | warnings=" + warningCount;
        }

        /// <summary>
        /// 构建单枚芯片的定义层诊断说明行。
        /// </summary>
        private static string BuildChipDefinitionDiagnosticLine(ChipDefinitionDiagnosticEntry diagnostic)
        {
            string chipLabel = string.IsNullOrWhiteSpace(diagnostic.ChipLabel) ? "(未知芯片)" : diagnostic.ChipLabel;
            string firstError = diagnostic.Errors != null && diagnostic.Errors.Count > 0
                ? diagnostic.Errors[0]
                : "-";
            string firstWarning = diagnostic.Warnings != null && diagnostic.Warnings.Count > 0
                ? diagnostic.Warnings[0]
                : "-";
            return chipLabel
                + " | valid=" + diagnostic.IsValid
                + " | firstError=" + firstError
                + " | firstWarning=" + firstWarning;
        }

        /// <summary>
        /// 把芯片定义校验消息翻译成说明层字符串。
        /// </summary>
        private static IReadOnlyList<string> TranslateValidationMessages(
            IReadOnlyList<BDP.Core.Chips.ChipDefinitionValidationMessage> messages)
        {
            List<string> result = new List<string>();
            if (messages == null)
            {
                return result;
            }

            for (int i = 0; i < messages.Count; i++)
            {
                BDP.Core.Chips.ChipDefinitionValidationMessage message = messages[i];
                if (message == null || string.IsNullOrWhiteSpace(message.Message))
                {
                    continue;
                }

                result.Add(message.Message);
            }

            return result;
        }
    }

}
