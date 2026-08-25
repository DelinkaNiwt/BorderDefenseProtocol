using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.CombatModel;
using BDP.Core.Semantics;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达来源收集器。
    /// 它只负责从 Trigger owner 真值收集并整理 ExpressionSourceMaterial。
    /// </summary>
    internal sealed class ExpressionSourceCollector
    {
        /// <summary>
        /// 来源声明提供器。
        /// </summary>
        private readonly IExpressionSourceDeclarationProvider declarationProvider;

        /// <summary>
        /// 条件评估器。
        /// </summary>
        private readonly DefaultExpressionConditionEvaluator conditionEvaluator;

        /// <summary>
        /// 用既有声明提供器与条件评估器构造来源收集器。
        /// </summary>
        internal ExpressionSourceCollector(
            IExpressionSourceDeclarationProvider declarationProvider,
            DefaultExpressionConditionEvaluator conditionEvaluator)
        {
            this.declarationProvider = declarationProvider;
            this.conditionEvaluator = conditionEvaluator;
        }

        /// <summary>
        /// 从当前 Trigger owner 读口收集全部来源材料。
        /// </summary>
        internal IReadOnlyList<ExpressionSourceMaterial> Collect(
            Pawn pawn,
            ITriggerLoadoutReader triggerLoadoutReader)
        {
            if (declarationProvider == null || triggerLoadoutReader == null)
            {
                return new List<ExpressionSourceMaterial>();
            }

            List<ExpressionSourceMaterial> result = new List<ExpressionSourceMaterial>();
            IEnumerable<ITriggerSlotState> activeSlots = triggerLoadoutReader.GetActiveSlots();
            if (activeSlots == null)
            {
                return result;
            }

            foreach (ITriggerSlotState slot in activeSlots)
            {
                CollectSlotMaterials(pawn, slot, triggerLoadoutReader, result);
            }

            return result;
        }

        /// <summary>
        /// 收集单个激活槽位的来源材料。
        /// </summary>
        private void CollectSlotMaterials(
            Pawn pawn,
            ITriggerSlotState slot,
            ITriggerLoadoutReader triggerLoadoutReader,
            List<ExpressionSourceMaterial> result)
        {
            if (slot == null || result == null || slot.LoadedChip == null || slot.IsBindingMirror)
            {
                return;
            }

            IReadOnlyList<ExpressionSourceDeclaration> declarations =
                declarationProvider.GetDeclarations(slot.LoadedChip, triggerLoadoutReader);
            if (declarations == null || declarations.Count == 0)
            {
                return;
            }

            for (int i = 0; i < declarations.Count; i++)
            {
                ExpressionSourceDeclaration declaration = declarations[i];
                if (declaration == null || string.IsNullOrWhiteSpace(declaration.Id))
                {
                    continue;
                }

                result.Add(BuildMaterial(
                    pawn,
                    slot,
                    declaration,
                    EvaluateConditions(pawn, slot, declaration)));
            }
        }

        /// <summary>
        /// 把一条来源声明翻译成运行时来源材料。
        /// </summary>
        private static ExpressionSourceMaterial BuildMaterial(
            Pawn pawn,
            ITriggerSlotState slot,
            ExpressionSourceDeclaration declaration,
            ExpressionConditionEvaluation conditionEvaluation)
        {
            ExpressionVerbHostSlot hostSlot = ResolveSingleSideHostSlot(slot, declaration);
            // 来源变体在语义上下文构建前解析，确保伤害来源名能带上业务侧标签。
            string sourceVariantKey = ResolveSourceVariantKey(
                slot.LoadedChip,
                out string sourceVariantLabel);
            return new ExpressionSourceMaterial
            {
                Id = BuildMaterialId(slot, declaration),
                Side = slot.Side,
                SlotIndex = slot.Index,
                ResultKind = declaration.ResultKind,
                WeaponMode = declaration.WeaponMode,
                SourceChip = slot.LoadedChip,
                SourceReference = BuildSourceReference(slot),
                RuntimePayload = BuildRuntimePayload(slot, declaration),
                DisplayLabel = declaration.DisplayLabel,
                ManualEntryIconTexPath = ResolveManualEntryIconTexPath(slot, declaration),
                VisualPresetDefName = declaration.VisualPresetDefName,
                VisualGraphicOverrideDefName = declaration.VisualGraphicOverrideDefName,
                CompositeVisualPresetDefName = declaration.CompositeVisualPresetDefName,
                ForceSuppressHostEquipment = declaration.ForceSuppressHostEquipment,
                VisualPriority = declaration.VisualPriority,
                RoleKey = declaration.RoleKey,
                VerbAttackRole = declaration.VerbAttackRole,
                Tags = declaration.Tags,
                Trion = declaration.Trion,
                ConditionEvaluation = conditionEvaluation,
                IsEnabled = conditionEvaluation != null && conditionEvaluation.IsSatisfied,
                ModeKey = declaration.ModeKey,
                SemanticContext = BuildSemanticContext(pawn, declaration, sourceVariantLabel),
                VerbHostKey = BuildVerbHostKey(slot, declaration, hostSlot),
                VerbHostSlot = hostSlot,
                ExecutionStyle = declaration.ExecutionStyle != null ? declaration.ExecutionStyle.Clone() : null,
                VerbProps = declaration.VerbProps,
                ResolvedVerbSpec = declaration.ResolvedVerbSpec,
                Tool = declaration.Tool,
                DeclaredTools = declaration.DeclaredTools != null ? new List<Tool>(declaration.DeclaredTools) : new List<Tool>(),
                DeclaredMeleeToolSurfaces = CloneMeleeToolSurfaces(declaration.DeclaredMeleeToolSurfaces),
                Maneuver = declaration.Maneuver,
                AbilityDefName = declaration.AbilityDefName,
                HediffDefName = declaration.HediffDefName,
                HediffApplyModeKey = declaration.HediffApplyModeKey,
                PassiveKey = declaration.PassiveKey,
                ExposedData = declaration.ExposedData,
                RangedModules = declaration.RangedModules != null ? CloneRangedModules(declaration.RangedModules) : new List<RangedModuleMountConfig>(),
                RangedModuleAugmentations = declaration.RangedModuleAugmentations != null
                    ? CloneRangedModuleAugmentations(declaration.RangedModuleAugmentations)
                    : new List<RangedModuleAugmentationConfig>(),
                SourceVariantKey = sourceVariantKey,
                SourceVariantLabel = sourceVariantLabel
            };
        }

        /// <summary>
        /// 构建单芯片表达来源的运行参数包。
        /// 这里统一把芯片本体参数和表达条目参数固定到材料上。
        /// </summary>
        private static ExpressionRuntimePayload BuildRuntimePayload(
            ITriggerSlotState slot,
            ExpressionSourceDeclaration declaration)
        {
            ExpressionChipSnapshot chipSnapshot = BuildChipSnapshot(slot);
            List<ExpressionChipSnapshot> sourceChips = new List<ExpressionChipSnapshot>();
            if (chipSnapshot != null)
            {
                sourceChips.Add(chipSnapshot.Clone());
            }

            return new ExpressionRuntimePayload
            {
                SourceChip = chipSnapshot,
                SourceChips = sourceChips,
                Combo = null,
                Entry = BuildEntrySnapshot(declaration),
                SourceReferences = BuildSourceReferences(slot)
            };
        }

        /// <summary>
        /// 构建来源芯片快照。
        /// 芯片契约读取失败时仍保留最小来源身份，避免四类结果失去追踪坐标。
        /// </summary>
        private static ExpressionChipSnapshot BuildChipSnapshot(ITriggerSlotState slot)
        {
            if (slot == null || slot.LoadedChip == null)
            {
                return null;
            }

            ChipDefinitionReadResult readResult = ChipSurfaceAccess.Read(slot.LoadedChip);
            ChipDefinitionContract contract = readResult != null ? readResult.Contract : null;
            ChipProfileContract profile = contract != null ? contract.Profile : null;
            ChipLoadoutContract loadout = contract != null ? contract.Loadout : null;

            return new ExpressionChipSnapshot
            {
                ChipThingId = slot.LoadedChip.ThingID,
                ChipDefName = slot.LoadedChip.def != null ? slot.LoadedChip.def.defName : null,
                Side = slot.Side,
                SlotIndex = slot.Index,
                ProfileCategoryDefName = profile != null && profile.Category != null
                    ? profile.Category.defName
                    : null,
                ProfileTagDefNames = BuildProfileTagDefNames(profile != null ? profile.Tags : null),
                LoadoutSlotRegion = loadout != null ? loadout.SlotRegion : (ChipSlotRegion?)null,
                LoadoutSlotOccupancy = loadout != null
                    ? loadout.SlotOccupancy
                    : (ChipSlotOccupancy?)null,
                ChipTrion = BuildChipTrionSnapshot(contract != null ? contract.Trion : null),
                DeclaredBlocks = contract != null && contract.DeclaredBlocks != null
                    ? new List<ChipDefinitionDeclaredBlock>(contract.DeclaredBlocks)
                    : new List<ChipDefinitionDeclaredBlock>()
            };
        }

        /// <summary>
        /// 把芯片画像标签转换为稳定 DefName 快照。
        /// </summary>
        private static IReadOnlyList<string> BuildProfileTagDefNames(
            IReadOnlyList<ChipTagDef> tags)
        {
            List<string> result = new List<string>();
            if (tags == null)
            {
                return result;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                ChipTagDef tag = tags[i];
                if (tag != null && !string.IsNullOrWhiteSpace(tag.defName))
                {
                    result.Add(tag.defName);
                }
            }

            return result;
        }

        /// <summary>
        /// 构建表达条目参数快照。
        /// </summary>
        private static ExpressionEntrySnapshot BuildEntrySnapshot(ExpressionSourceDeclaration declaration)
        {
            if (declaration == null)
            {
                return null;
            }

            return new ExpressionEntrySnapshot
            {
                Id = declaration.Id,
                ResultKind = declaration.ResultKind,
                WeaponMode = declaration.WeaponMode,
                DisplayLabel = declaration.DisplayLabel,
                ManualEntryIconTexPath = declaration.ManualEntryIconTexPath,
                VisualPresetDefName = declaration.VisualPresetDefName,
                VisualGraphicOverrideDefName = declaration.VisualGraphicOverrideDefName,
                CompositeVisualPresetDefName = declaration.CompositeVisualPresetDefName,
                ForceSuppressHostEquipment = declaration.ForceSuppressHostEquipment,
                VisualPriority = declaration.VisualPriority,
                RoleKey = declaration.RoleKey,
                VerbAttackRole = declaration.VerbAttackRole,
                Tags = declaration.Tags != null ? new List<string>(declaration.Tags) : new List<string>(),
                ExpressionTrion = BuildExpressionTrionSnapshot(declaration.Trion),
                ModeKey = declaration.ModeKey,
                AbilityDefName = declaration.AbilityDefName,
                HediffDefName = declaration.HediffDefName,
                HediffApplyModeKey = declaration.HediffApplyModeKey,
                PassiveKey = declaration.PassiveKey
            };
        }

        /// <summary>
        /// 构建芯片本体 Trion 快照。
        /// </summary>
        private static ExpressionChipTrionSnapshot BuildChipTrionSnapshot(ChipTrionContract trion)
        {
            if (trion == null)
            {
                return null;
            }

            return new ExpressionChipTrionSnapshot
            {
                CapacityCost = trion.CapacityCost,
                ActivationCost = trion.ActivationCost
            };
        }

        /// <summary>
        /// 构建表达级 Trion 快照。
        /// </summary>
        private static ExpressionSourceTrionSnapshot BuildExpressionTrionSnapshot(ExpressionSourceTrionConfig trion)
        {
            if (trion == null)
            {
                return null;
            }

            return new ExpressionSourceTrionSnapshot
            {
                UseCost = trion.UseCost,
                MinimumRequired = trion.MinimumRequired,
                SustainCostBySourceCount = CloneSustainCostBySourceCount(trion.SustainCostBySourceCount)
            };
        }

        /// <summary>
        /// 深复制表达持续费用档位。
        /// </summary>
        private static IReadOnlyList<ExpressionSustainCostBySourceCountConfig> CloneSustainCostBySourceCount(
            IReadOnlyList<ExpressionSustainCostBySourceCountConfig> source)
        {
            List<ExpressionSustainCostBySourceCountConfig> result =
                new List<ExpressionSustainCostBySourceCountConfig>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ExpressionSustainCostBySourceCountConfig row = source[i];
                if (row == null)
                {
                    continue;
                }

                result.Add(new ExpressionSustainCostBySourceCountConfig
                {
                    SourceCount = row.SourceCount,
                    TotalPerSecond = row.TotalPerSecond
                });
            }

            return result;
        }

        /// <summary>
        /// 构建来源引用集合。
        /// </summary>
        private static IReadOnlyList<ExpressionSourceReference> BuildSourceReferences(ITriggerSlotState slot)
        {
            List<ExpressionSourceReference> result = new List<ExpressionSourceReference>();
            ExpressionSourceReference reference = BuildSourceReference(slot);
            if (reference != null)
            {
                result.Add(reference);
            }

            return result;
        }

        /// <summary>
        /// 为当前材料构建最小来源追踪。
        /// 它只记录回到正式槽位所需的最小坐标，不携带额外业务语义。
        /// </summary>
        private static ExpressionSourceReference BuildSourceReference(ITriggerSlotState slot)
        {
            if (slot == null || slot.LoadedChip == null)
            {
                return null;
            }

            return new ExpressionSourceReference
            {
                ChipThingId = slot.LoadedChip.ThingID,
                ChipDefName = slot.LoadedChip.def != null ? slot.LoadedChip.def.defName : null,
                Side = slot.Side,
                SlotIndex = slot.Index
            };
        }

        /// <summary>
        /// 评估当前来源声明的成立条件。
        /// </summary>
        private ExpressionConditionEvaluation EvaluateConditions(
            Pawn pawn,
            ITriggerSlotState slot,
            ExpressionSourceDeclaration declaration)
        {
            if (conditionEvaluator == null)
            {
                return new ExpressionConditionEvaluation
                {
                    IsSatisfied = declaration != null
                        && (declaration.Conditions == null || declaration.Conditions.Count == 0),
                    HasUnknownConditions = declaration != null
                        && declaration.Conditions != null
                        && declaration.Conditions.Count > 0,
                    Notes = declaration != null
                        && declaration.Conditions != null
                        && declaration.Conditions.Count > 0
                        ? new List<string> { "缺少条件评估器，当前条件来源不应被视为正式成立。" }
                        : new List<string>()
                };
            }

            return conditionEvaluator.Evaluate(pawn, slot, declaration);
        }

        /// <summary>
        /// 为当前来源材料生成稳定标识。
        /// </summary>
        private static string BuildMaterialId(ITriggerSlotState slot, ExpressionSourceDeclaration declaration)
        {
            string chipId = slot != null && slot.LoadedChip != null ? slot.LoadedChip.ThingID : "nullChip";
            string declarationId = declaration != null ? declaration.Id : "nullDeclaration";
            return chipId + ":" + declarationId;
        }

        /// <summary>
        /// 为当前来源材料构建最小语义上下文。
        /// 来源变体标签会并入 DisplayLabel 后缀，确保来源名保留完整身份。
        /// </summary>
        private static ISemanticContext BuildSemanticContext(
            Pawn pawn,
            ExpressionSourceDeclaration declaration,
            string sourceVariantLabel)
        {
            string displayLabel = declaration != null ? declaration.DisplayLabel : null;
            return new SemanticContext
            {
                Id = declaration != null ? declaration.Id : null,
                DisplayLabel = MergeSourceVariantSuffix(displayLabel, sourceVariantLabel),
                SourceKind = declaration != null ? declaration.SemanticSourceKind : SemanticSourceKind.Unknown,
                ReasonKey = declaration != null ? declaration.Id : null,
                Instigator = pawn
            };
        }

        /// <summary>
        /// 把来源变体标签合并到显示名后缀。
        /// 格式与 UI 层保持一致："显示名[变体名]"，如"小行星[手枪型]"。
        /// 无来源变体时直接返回原始显示名。
        /// </summary>
        private static string MergeSourceVariantSuffix(
            string displayLabel,
            string sourceVariantLabel)
        {
            if (string.IsNullOrWhiteSpace(displayLabel)
                || string.IsNullOrWhiteSpace(sourceVariantLabel))
            {
                return displayLabel;
            }

            return displayLabel + "[" + sourceVariantLabel + "]";
        }

        /// <summary>
        /// 为单侧 Verb 来源解析固定宿主入口。
        /// </summary>
        private static ExpressionVerbHostSlot ResolveSingleSideHostSlot(
            ITriggerSlotState slot,
            ExpressionSourceDeclaration declaration)
        {
            if (slot == null
                || declaration == null
                || declaration.ResultKind != ExpressionResultKind.Verb)
            {
                return ExpressionVerbHostSlot.None;
            }

            switch (slot.Side)
            {
                case TriggerSide.Main:
                    return declaration.VerbAttackRole == VerbAttackRole.Secondary
                        ? ExpressionVerbHostSlot.MainSecondaryVerb
                        : ExpressionVerbHostSlot.MainPrimaryVerb;
                case TriggerSide.Sub:
                    return declaration.VerbAttackRole == VerbAttackRole.Secondary
                        ? ExpressionVerbHostSlot.SubSecondaryVerb
                        : ExpressionVerbHostSlot.SubPrimaryVerb;
                default:
                    return ExpressionVerbHostSlot.None;
            }
        }

        /// <summary>
        /// 为当前材料构建宿主来源键。
        /// </summary>
        private static ExpressionVerbHostKey BuildVerbHostKey(
            ITriggerSlotState slot,
            ExpressionSourceDeclaration declaration,
            ExpressionVerbHostSlot hostSlot)
        {
            if (slot == null
                || slot.LoadedChip == null
                || declaration == null
                || declaration.ResultKind != ExpressionResultKind.Verb
                || hostSlot == ExpressionVerbHostSlot.None)
            {
                return null;
            }

            return new ExpressionVerbHostKey
            {
                ChipThingId = slot.LoadedChip.ThingID,
                Side = slot.Side,
                HostSlot = hostSlot,
                ModeKey = declaration.ModeKey
            };
        }

        /// <summary>
        /// 对近战 Tool 表面做最小浅复制，避免共享可变引用。
        /// </summary>
        private static IReadOnlyList<MeleeToolSurface> CloneMeleeToolSurfaces(
            IReadOnlyList<MeleeToolSurface> surfaces)
        {
            List<MeleeToolSurface> result = new List<MeleeToolSurface>();
            if (surfaces == null)
            {
                return result;
            }

            for (int i = 0; i < surfaces.Count; i++)
            {
                MeleeToolSurface surface = surfaces[i];
                if (surface == null)
                {
                    continue;
                }

                result.Add(new MeleeToolSurface
                {
                    Tool = surface.Tool,
                    VerbProps = surface.VerbProps,
                    Maneuver = surface.Maneuver,
                    DamageDef = surface.DamageDef,
                    DeclaredIndex = surface.DeclaredIndex
                });
            }

            return result;
        }

        /// <summary>
        /// 对模块挂载快照做最小复制，避免运行时回写上游声明对象。
        /// </summary>
        private static IReadOnlyList<RangedModuleMountConfig> CloneRangedModules(
            IReadOnlyList<RangedModuleMountConfig> modules)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (modules == null)
            {
                return result;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                RangedModuleMountConfig module = modules[i];
                if (module == null)
                {
                    continue;
                }

                result.Add(module.Clone());
            }

            return result;
        }

        /// <summary>
        /// 对开放式远程增强声明做最小快照复制。
        /// </summary>
        private static IReadOnlyList<RangedModuleAugmentationConfig> CloneRangedModuleAugmentations(
            IReadOnlyList<RangedModuleAugmentationConfig> augmentations)
        {
            List<RangedModuleAugmentationConfig> result =
                new List<RangedModuleAugmentationConfig>();
            if (augmentations == null)
            {
                return result;
            }

            for (int index = 0; index < augmentations.Count; index++)
            {
                RangedModuleAugmentationConfig augmentation = augmentations[index];
                if (augmentation != null)
                {
                    result.Add(augmentation.Clone());
                }
            }

            return result;
        }

        /// <summary>
        /// 解析单芯片来源当前应使用的手动入口按钮贴图路径。
        /// </summary>
        private static string ResolveManualEntryIconTexPath(
            ITriggerSlotState slot,
            ExpressionSourceDeclaration declaration)
        {
            if (declaration != null && !string.IsNullOrWhiteSpace(declaration.ManualEntryIconTexPath))
            {
                return declaration.ManualEntryIconTexPath;
            }

            if (slot == null || slot.LoadedChip == null || slot.LoadedChip.def == null)
            {
                return null;
            }

            return slot.LoadedChip.def.graphicData != null ? slot.LoadedChip.def.graphicData.texPath : null;
        }

        /// <summary>
        /// 从芯片 Thing 的中性来源提供器解析当前来源变体。
        /// </summary>
        private static string ResolveSourceVariantKey(
            Thing chip,
            out string sourceVariantLabel)
        {
            sourceVariantLabel = null;
            if (chip == null)
            {
                return null;
            }

            ChipSourceReferenceSnapshot source = ChipInstanceSurfaceAccess.ReadSourceReference(chip);
            sourceVariantLabel = source.SourceVariantLabel;
            return source.SourceVariantKey;
        }
    }
}
