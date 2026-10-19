using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Content.Assembly.ChipManufacturing.Thing;
using BDP.Content.Information;
using BDP.Content.Information.Combos;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using BDP.Core.Information;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配台的芯片详情模型入口。
    /// 成品芯片优先从持久化组合记录重建；其它芯片才使用通用实例适配器。
    /// </summary>
    internal static class ChipAssemblyDetailModelBuilder
    {
        /// <summary>枪手信息图基准固定使用的构型 DefName，不参与制造构型选择。</summary>
        private const string InformationComparisonBaselineArmamentFormDefName =
            "BDP_ArmamentForm_TrionCube";

        /// <summary>Trion 立方体只作为枪手与射手图表的理论比较修正，不走制造准入解析。</summary>
        private const string GunnerProfessionDefName = "BDP_ChipProfession_Gunner";

        /// <summary>射手功能也使用 Trion 立方体修正作为图表理论比较基准。</summary>
        private const string ShooterProfessionDefName = "BDP_ChipProfession_Shooter";

        /// <summary>为制造台当前解析结果建立与装配台相同的当前值/比较基准模型。</summary>
        internal static ChipInformationComparisonModel BuildManufacturingComparison(
            ChipCombinationResolution resolution)
        {
            if (resolution == null
                || resolution.Status != ChipCombinationResolutionStatus.Valid
                || resolution.ResolvedConfig == null)
            {
                return null;
            }

            ChipInformationModel current = ManufacturedChipInformationModelBuilder.BuildForDetail(resolution);
            if (current == null)
            {
                return null;
            }

            ChipInformationComparisonBaselineMode baselineMode =
                ResolveRootBaselineMode(resolution.SourceProfessionKey);
            ChipCombinationResolution baselineResolution = ResolveComparisonBaseline(
                resolution,
                baselineMode);
            ChipInformationModel baseline = baselineResolution == null
                ? null
                : ManufacturedChipInformationModelBuilder.BuildForDetail(baselineResolution);
            return ChipInformationComparisonModel.Create(
                current,
                baseline,
                baselineResolution?.ArmamentForm?.ResolvedLabel,
                baselineMode,
                resolution.ResolvedConfig,
                CreateSourceReference(resolution));
        }

        /// <summary>
        /// 尝试为一枚已选芯片构建当前值与比较基准配对模型。
        /// </summary>
        internal static bool TryBuild(
            Thing chip,
            out ChipInformationComparisonModel comparison)
        {
            return TryBuild(
                chip,
                out comparison,
                ChipInformationRuntimeInputAdapter.CreateStatic(),
                null);
        }

        /// <summary>按芯片职业与声明数据自动选择图表基准预设。</summary>
        internal static bool TryBuild(
            Thing chip,
            out ChipInformationComparisonModel comparison,
            ChipInformationRuntimeState runtime)
        {
            return TryBuild(chip, out comparison, runtime, null);
        }

        /// <summary>按只读已发布表达快照构建一枚芯片的详情模型。</summary>
        internal static bool TryBuild(
            Thing chip,
            out ChipInformationComparisonModel comparison,
            ChipInformationRuntimeState runtime,
            ExpressionPublishedProjectionSnapshot publishedProjection)
        {
            comparison = null;
            if (chip == null)
            {
                return false;
            }

            CompManufacturedChip manufactured = chip.TryGetComp<CompManufacturedChip>();
            if (manufactured != null && manufactured.CombinationRecord != null)
            {
                ChipCombinationResolution resolution =
                    new ChipCombinationResolver().Resolve(manufactured.CombinationRecord);
                if (resolution.Status != ChipCombinationResolutionStatus.Valid)
                {
                    return false;
                }

                ChipInformationModel current =
                    ManufacturedChipInformationModelBuilder.BuildForDetail(
                        resolution,
                        chip.ThingID,
                        runtime,
                        publishedProjection);
                if (current == null)
                {
                    return false;
                }

                ChipInformationComparisonBaselineMode baselineMode =
                    ResolveRootBaselineMode(resolution.SourceProfessionKey);
                ChipCombinationResolution baselineResolution = ResolveComparisonBaseline(
                    resolution,
                    baselineMode);
                ChipInformationModel baseline = baselineResolution == null
                    ? null
                    : ManufacturedChipInformationModelBuilder.BuildForDetail(baselineResolution);
                comparison = ChipInformationComparisonModel.Create(
                    current,
                    baseline,
                    baselineResolution?.ArmamentForm?.ResolvedLabel,
                    baselineMode,
                    resolution.ResolvedConfig,
                    CreateSourceReference(resolution));
                return true;
            }

            ChipInformationRequest request;
            if (!ChipInformationInstanceRequestAdapter.TryCreate(
                chip,
                runtime,
                runtime?.ActiveModeKey,
                runtime?.ActiveStanceKey,
                out request)
                || request == null)
            {
                return false;
            }

            ChipSourceReferenceSnapshot sourceReference =
                ChipInstanceSurfaceAccess.ReadSourceReference(chip);
            IReadOnlyList<ChipInformationExtensionReference> runtimeReferences =
                ComboChipInformationReferenceProjector.Create(
                    sourceReference,
                    request.EffectiveConfig,
                    chip.ThingID,
                    runtime?.ActiveModeKey,
                    runtime?.ActiveStanceKey,
                    publishedProjection);
            request = AppendExtensionReferences(request, runtimeReferences);

            ChipInformationModel model = ChipInformationSurfaceAccess.Read(request);
            if (model == null)
            {
                return false;
            }

            ChipInformationComparisonBaselineMode instanceBaselineMode = ResolveRootBaselineMode(
                sourceReference?.SourceProfessionKey ?? request.Identity?.SourceProfessionKey);
            ChipInformationModel instanceBaseline = BuildInstanceComparisonBaseline(
                request,
                instanceBaselineMode);
            comparison = ChipInformationComparisonModel.Create(
                model,
                instanceBaseline,
                instanceBaselineMode == ChipInformationComparisonBaselineMode.TrionCubeAdjustedFunction
                    ? FindInformationComparisonBaselineArmamentForm()?.ResolvedLabel
                    : null,
                instanceBaselineMode,
                request.EffectiveConfig,
                sourceReference);
            return true;
        }

        /// <summary>复制成品解析结果的正式来源事实，供理论组合准入与求值共用。</summary>
        private static ChipSourceReferenceSnapshot CreateSourceReference(ChipCombinationResolution resolution)
        {
            List<string> keys = new List<string>();
            foreach (ChipManufacturingPresetDef function in resolution.FunctionDefs) keys.Add(function.defName);
            return new ChipSourceReferenceSnapshot
            {
                OrderedSourceKeys = keys.AsReadOnly(),
                SourceProfessionKey = resolution.SourceProfessionKey,
                SourceVariantKey = resolution.ArmamentForm?.defName
            };
        }

        /// <summary>将外部适配器新贡献的引用追加到不可变信息请求。</summary>
        private static ChipInformationRequest AppendExtensionReferences(
            ChipInformationRequest request,
            IReadOnlyList<ChipInformationExtensionReference> additionalReferences)
        {
            if (request == null || additionalReferences == null || additionalReferences.Count == 0)
            {
                return request;
            }

            List<ChipInformationExtensionReference> references =
                new List<ChipInformationExtensionReference>();
            if (request.ExtensionReferences != null)
            {
                for (int index = 0; index < request.ExtensionReferences.Count; index++)
                {
                    if (request.ExtensionReferences[index] != null)
                    {
                        references.Add(request.ExtensionReferences[index]);
                    }
                }
            }

            for (int index = 0; index < additionalReferences.Count; index++)
            {
                ChipInformationExtensionReference candidate = additionalReferences[index];
                if (candidate == null || ContainsReference(references, candidate))
                {
                    continue;
                }

                references.Add(candidate);
            }

            return new ChipInformationRequest(
                request.Identity,
                request.DeclaredConfig,
                request.EffectiveConfig,
                request.Runtime,
                request.CurrentModeKey,
                request.CurrentStanceKey,
                request.Descriptions,
                request.AdditionalBlocks,
                references,
                request.FeaturePropertySources);
        }

        /// <summary>按提供方、引用类型、目标和上下文判断引用是否已存在。</summary>
        private static bool ContainsReference(
            IReadOnlyList<ChipInformationExtensionReference> references,
            ChipInformationExtensionReference candidate)
        {
            for (int index = 0; index < references.Count; index++)
            {
                ChipInformationExtensionReference existing = references[index];
                if (existing != null
                    && existing.ProviderKey == candidate.ProviderKey
                    && existing.ReferenceKindKey == candidate.ReferenceKindKey
                    && existing.ReferenceKey == candidate.ReferenceKey
                    && existing.ContextKey == candidate.ContextKey
                    && existing.RelationRoleKey == candidate.RelationRoleKey)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 按图表预设从功能声明构建理论基准；此处不检查制造准入。
        /// </summary>
        private static ChipCombinationResolution ResolveComparisonBaseline(
            ChipCombinationResolution sourceResolution,
            ChipInformationComparisonBaselineMode baselineMode)
        {
            if (sourceResolution == null)
            {
                return null;
            }
            if (baselineMode == ChipInformationComparisonBaselineMode.HostFunctionResult)
            {
                return null;
            }

            ChipDefinitionConfig declaredConfig =
                sourceResolution.DeclaredConfig ?? sourceResolution.ResolvedConfig;
            if (declaredConfig == null)
            {
                return null;
            }

            ChipArmamentFormDef baselineArmamentForm = null;
            ChipDefinitionConfig effectiveConfig = declaredConfig;
            if (baselineMode == ChipInformationComparisonBaselineMode.TrionCubeAdjustedFunction)
            {
                baselineArmamentForm = FindInformationComparisonBaselineArmamentForm();
                if (baselineArmamentForm == null)
                {
                    return null;
                }

                effectiveConfig = ChipConfigurationMergeService.CloneConfig(declaredConfig);
                ChipArmamentFormApplicationService.Apply(effectiveConfig, baselineArmamentForm);
            }

            return new ChipCombinationResolution
            {
                Status = ChipCombinationResolutionStatus.Valid,
                ResolvedLabel = sourceResolution.ResolvedLabel,
                SourceProfessionKey = sourceResolution.SourceProfessionKey,
                DeclaredConfig = declaredConfig,
                ResolvedConfig = effectiveConfig,
                FunctionDefs = sourceResolution.FunctionDefs,
                ArmamentForm = baselineArmamentForm,
                FailureReasons = new List<ChipCombinationFailureReason>()
            };
        }

        /// <summary>按指定声明配置构建非成品芯片的图表比较基准。</summary>
        private static ChipInformationModel BuildInstanceComparisonBaseline(
            ChipInformationRequest sourceRequest,
            ChipInformationComparisonBaselineMode baselineMode)
        {
            ChipDefinitionConfig declaredConfig =
                sourceRequest?.DeclaredConfig ?? sourceRequest?.EffectiveConfig;
            if (declaredConfig == null)
            {
                return null;
            }

            ChipDefinitionConfig effectiveConfig = declaredConfig;
            List<ChipInformationPropertySource> featurePropertySources = null;
            List<ChipInformationFactBlock> additionalBlocks = null;
            if (baselineMode == ChipInformationComparisonBaselineMode.TrionCubeAdjustedFunction)
            {
                ChipArmamentFormDef armamentForm = FindInformationComparisonBaselineArmamentForm();
                if (armamentForm == null)
                {
                    return null;
                }

                effectiveConfig = ChipConfigurationMergeService.CloneConfig(declaredConfig);
                ChipArmamentFormApplicationService.Apply(effectiveConfig, armamentForm);
                featurePropertySources = ChipArmamentFormInformationAdapter.CreateFeaturePropertySources(
                    armamentForm);
                additionalBlocks = ChipArmamentFormInformationAdapter.CreateBlocks(armamentForm);
            }

            return ChipInformationSurfaceAccess.Read(new ChipInformationRequest(
                sourceRequest.Identity,
                declaredConfig,
                effectiveConfig,
                sourceRequest.Runtime,
                sourceRequest.CurrentModeKey,
                sourceRequest.CurrentStanceKey,
                sourceRequest.Descriptions,
                additionalBlocks ?? sourceRequest.AdditionalBlocks,
                sourceRequest.ExtensionReferences,
                featurePropertySources));
        }

        /// <summary>枪手与射手功能采用 Trion 立方体修正基准，其它功能采用自身定义基准。</summary>
        private static ChipInformationComparisonBaselineMode ResolveRootBaselineMode(
            string sourceProfessionKey)
        {
            bool usesTrionCubeBaseline = string.Equals(
                    sourceProfessionKey,
                    GunnerProfessionDefName,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    sourceProfessionKey,
                    ShooterProfessionDefName,
                    StringComparison.OrdinalIgnoreCase);

            return usesTrionCubeBaseline
                ? ChipInformationComparisonBaselineMode.TrionCubeAdjustedFunction
                : ChipInformationComparisonBaselineMode.DeclaredDefinition;
        }

        /// <summary>查找仅供图表理论比较使用的 Trion 立方体构型定义。</summary>
        private static ChipArmamentFormDef FindInformationComparisonBaselineArmamentForm()
        {
            return ChipManufacturingDefLookup.FindArmamentForm(
                InformationComparisonBaselineArmamentFormDefName);
        }
    }
}
