using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 从显式功能声明和来源 Worker 生成功能解析树。
    /// </summary>
    public sealed class ChipFeatureResolver
    {
        /// <summary>解析一组功能声明。</summary>
        public ChipFeatureResolutionResult Resolve(
            IEnumerable<ChipFeatureDeclaration> declarations,
            ChipFeatureReadContext context,
            int maximumDepth = ChipFeatureDefinitionValidator.MaximumDepth)
        {
            List<ChipInformationNode> features = new List<ChipInformationNode>();
            List<ChipInformationDiagnostic> diagnostics = new List<ChipInformationDiagnostic>();
            if (declarations == null)
            {
                return new ChipFeatureResolutionResult(features, diagnostics);
            }

            int index = 0;
            foreach (ChipFeatureDeclaration declaration in declarations)
            {
                if (!IsDeclarationActive(declaration, context?.ActiveEntryIds))
                {
                    index++;
                    continue;
                }

                ChipInformationNode feature = ResolveDeclaration(
                    declaration,
                    context,
                    null,
                    0,
                    maximumDepth,
                    diagnostics);
                if (feature != null)
                {
                    features.Add(feature);
                }

                index++;
            }

            return new ChipFeatureResolutionResult(features, diagnostics);
        }

        /// <summary>递归解析单项功能声明。</summary>
        private ChipInformationNode ResolveDeclaration(
            ChipFeatureDeclaration declaration,
            ChipFeatureReadContext context,
            string parentPath,
            int depth,
            int maximumDepth,
            List<ChipInformationDiagnostic> diagnostics)
        {
            if (declaration == null || declaration.Feature == null)
            {
                return null;
            }

            string path = string.IsNullOrWhiteSpace(parentPath)
                ? declaration.Id
                : parentPath + "/" + declaration.Id;
            if (depth > maximumDepth)
            {
                diagnostics.Add(new ChipInformationDiagnostic(
                    "FeatureDepthExceeded",
                    path,
                    null,
                    ChipInformationDiagnosticSeverity.Error,
                    "功能子树超过允许深度，已停止展开。"));
                return null;
            }

            List<ChipInformationProperty> properties = ResolveProperties(
                declaration.Feature.Properties,
                declaration.Sources,
                context,
                path,
                diagnostics);

            List<ChipInformationNode> groups = ResolveGroups(
                declaration,
                context,
                path,
                diagnostics);

            List<ChipInformationNode> subgroups = ResolveSubgroups(
                declaration.Feature.Subgroups,
                declaration.Subgroups,
                path + "/subgroups/",
                context,
                "FeatureSubgroupNotAccepted",
                "功能语义未声明允许该子组，已忽略。",
                diagnostics);

            List<ChipInformationNode> children = new List<ChipInformationNode>();
            if (declaration.Children != null)
            {
                for (int index = 0; index < declaration.Children.Count; index++)
                {
                    if (!IsDeclarationActive(
                        declaration.Children[index],
                        context?.ActiveEntryIds))
                    {
                        continue;
                    }

                    ChipInformationNode child = ResolveDeclaration(
                        declaration.Children[index],
                        context,
                        path,
                        depth + 1,
                        maximumDepth,
                        diagnostics);
                    if (child != null)
                    {
                        children.Add(child);
                    }
                }
            }

            List<ChipInformationNode> featureChildren = new List<ChipInformationNode>();
            featureChildren.AddRange(groups);
            featureChildren.AddRange(subgroups);
            featureChildren.AddRange(children);
            return new ChipInformationNode(
                path,
                "feature",
                declaration.Feature.defName,
                declaration.LabelKey,
                declaration.Feature.DisplayOrder,
                properties,
                featureChildren);
        }

        /// <summary>按功能允许的信息组槽位顺序解析组实例。</summary>
        private List<ChipInformationNode> ResolveGroups(
            ChipFeatureDeclaration declaration,
            ChipFeatureReadContext context,
            string featurePath,
            List<ChipInformationDiagnostic> diagnostics)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (declaration?.Groups == null || declaration.Groups.Count == 0)
            {
                return result;
            }

            HashSet<ChipFeatureGroupDeclaration> resolvedDeclarations =
                new HashSet<ChipFeatureGroupDeclaration>();
            List<ChipFeatureGroupSlot> slots = declaration.Feature.Groups == null
                ? new List<ChipFeatureGroupSlot>()
                : new List<ChipFeatureGroupSlot>(declaration.Feature.Groups);
            slots.Sort((first, second) => (first?.Order ?? 0).CompareTo(second?.Order ?? 0));

            for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
            {
                ChipFeatureGroupDef allowedGroup = slots[slotIndex]?.Group;
                if (allowedGroup == null)
                {
                    continue;
                }

                for (int declarationIndex = 0;
                    declarationIndex < declaration.Groups.Count;
                    declarationIndex++)
                {
                    ChipFeatureGroupDeclaration groupDeclaration =
                        declaration.Groups[declarationIndex];
                    if (groupDeclaration == null
                        || groupDeclaration.Group != allowedGroup
                        || !resolvedDeclarations.Add(groupDeclaration))
                    {
                        continue;
                    }

                    string groupPath = featurePath + "/groups/" + groupDeclaration.Id;
                    if (!IsGroupDeclarationActive(
                        groupDeclaration,
                        context?.ActiveEntryIds))
                    {
                        continue;
                    }

                    List<ChipInformationProperty> groupProperties = ResolveProperties(
                        groupDeclaration.Group.Properties,
                        groupDeclaration.Sources,
                        context,
                        groupPath,
                        diagnostics);
                    List<ChipInformationNode> subgroups = ResolveSubgroups(
                        groupDeclaration.Group.Subgroups,
                        groupDeclaration.Subgroups,
                        groupPath + "/groups/",
                        context,
                        "SubgroupNotAccepted",
                        "父信息组未声明允许该子组，已忽略。",
                        diagnostics);
                    result.Add(new ChipInformationNode(
                        groupPath,
                        "feature_group",
                        groupDeclaration.Group.defName,
                        groupDeclaration.LabelKey,
                        groupDeclaration.Group.DisplayOrder,
                        groupProperties,
                        subgroups));
                }
            }

            for (int declarationIndex = 0;
                declarationIndex < declaration.Groups.Count;
                declarationIndex++)
            {
                ChipFeatureGroupDeclaration groupDeclaration =
                    declaration.Groups[declarationIndex];
                if (groupDeclaration == null || resolvedDeclarations.Contains(groupDeclaration))
                {
                    continue;
                }

                string groupPath = featurePath + "/groups/" + groupDeclaration.Id;
                diagnostics.Add(new ChipInformationDiagnostic(
                    "FeatureGroupNotAccepted",
                    groupPath,
                    groupDeclaration.Group?.defName,
                    ChipInformationDiagnosticSeverity.Warning,
                    "功能语义未声明允许该信息组，已忽略。"));
            }

            return result;
        }

        /// <summary>按当前功能或信息组允许的槽位顺序解析其一层子组。</summary>
        private List<ChipInformationNode> ResolveSubgroups(
            IList<ChipFeatureGroupSlot> allowedSlots,
            IList<ChipFeatureSubgroupDeclaration> declarations,
            string subgroupPathPrefix,
            ChipFeatureReadContext context,
            string notAcceptedCode,
            string notAcceptedMessage,
            List<ChipInformationDiagnostic> diagnostics)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (declarations == null || declarations.Count == 0)
            {
                return result;
            }

            HashSet<ChipFeatureSubgroupDeclaration> resolvedDeclarations =
                new HashSet<ChipFeatureSubgroupDeclaration>();
            List<ChipFeatureGroupSlot> slots = allowedSlots == null
                ? new List<ChipFeatureGroupSlot>()
                : new List<ChipFeatureGroupSlot>(allowedSlots);
            slots.Sort((first, second) =>
                (first?.Order ?? 0).CompareTo(second?.Order ?? 0));

            for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
            {
                ChipFeatureGroupDef allowedGroup = slots[slotIndex]?.Group;
                if (allowedGroup == null)
                {
                    continue;
                }

                for (int declarationIndex = 0;
                    declarationIndex < declarations.Count;
                    declarationIndex++)
                {
                    ChipFeatureSubgroupDeclaration subgroup = declarations[declarationIndex];
                    if (subgroup == null
                        || subgroup.Group != allowedGroup
                        || !resolvedDeclarations.Add(subgroup))
                    {
                        continue;
                    }

                    string subgroupPath = subgroupPathPrefix + subgroup.Id;
                    if (!IsSourceSetActive(subgroup.Sources, context?.ActiveEntryIds))
                    {
                        continue;
                    }

                    List<ChipInformationProperty> properties = ResolveProperties(
                        subgroup.Group.Properties,
                        subgroup.Sources,
                        context,
                        subgroupPath,
                        diagnostics);
                    result.Add(new ChipInformationNode(
                        subgroupPath,
                        "feature_subgroup",
                        subgroup.Group.defName,
                        subgroup.LabelKey,
                        subgroup.Group.DisplayOrder,
                        properties,
                        null));
                }
            }

            for (int declarationIndex = 0;
                declarationIndex < declarations.Count;
                declarationIndex++)
            {
                ChipFeatureSubgroupDeclaration subgroup = declarations[declarationIndex];
                if (subgroup == null || resolvedDeclarations.Contains(subgroup))
                {
                    continue;
                }

                string subgroupPath = subgroupPathPrefix + (subgroup.Id ?? string.Empty);
                diagnostics.Add(new ChipInformationDiagnostic(
                    notAcceptedCode,
                    subgroupPath,
                    subgroup.Group?.defName,
                    ChipInformationDiagnosticSeverity.Warning,
                    notAcceptedMessage));
            }

            return result;
        }

        /// <summary>父组在当前上下文有来源或至少一个可用子组时才参与解析。</summary>
        private static bool IsGroupDeclarationActive(
            ChipFeatureGroupDeclaration declaration,
            IReadOnlyList<string> activeEntryIds)
        {
            if (activeEntryIds == null)
            {
                return true;
            }

            bool hasSources = declaration?.Sources != null && declaration.Sources.Count > 0;
            if (hasSources && IsSourceSetActive(declaration.Sources, activeEntryIds))
            {
                return true;
            }

            if (declaration?.Subgroups != null)
            {
                for (int index = 0; index < declaration.Subgroups.Count; index++)
                {
                    if (IsSourceSetActive(
                        declaration.Subgroups[index]?.Sources,
                        activeEntryIds))
                    {
                        return true;
                    }
                }
            }

            return !hasSources && (declaration?.Subgroups == null
                || declaration.Subgroups.Count == 0);
        }

        /// <summary>复用同一属性来源读取、槽位过滤和贡献裁定流程。</summary>
        private List<ChipInformationProperty> ResolveProperties(
            IList<ChipFeaturePropertySlot> slots,
            IList<ChipFeatureSourceConfig> sources,
            ChipFeatureReadContext context,
            string path,
            List<ChipInformationDiagnostic> diagnostics)
        {
            Dictionary<ChipFeaturePropertyDef, List<ChipFeatureValueContribution>> contributions =
                new Dictionary<ChipFeaturePropertyDef, List<ChipFeatureValueContribution>>();
            ResolveSources(sources, context, path, contributions, diagnostics);
            Dictionary<ChipFeaturePropertyDef, List<ChipFeatureValueContribution>> baseContributions =
                ResolveBaseSources(sources, context, path);

            HashSet<ChipFeaturePropertyDef> acceptedProperties =
                new HashSet<ChipFeaturePropertyDef>();
            if (slots != null)
            {
                for (int index = 0; index < slots.Count; index++)
                {
                    ChipFeaturePropertySlot slot = slots[index];
                    if (slot?.Property != null)
                    {
                        acceptedProperties.Add(slot.Property);
                    }
                }
            }

            foreach (KeyValuePair<ChipFeaturePropertyDef, List<ChipFeatureValueContribution>> pair in contributions)
            {
                if (!acceptedProperties.Contains(pair.Key))
                {
                    diagnostics.Add(new ChipInformationDiagnostic(
                        "FeaturePropertyNotAccepted",
                        path,
                        pair.Key.defName,
                        ChipInformationDiagnosticSeverity.Warning,
                        "来源 Worker 返回的属性不在该语义或信息组的允许槽位中，已忽略。"));
                }
            }

            List<ChipInformationProperty> properties = new List<ChipInformationProperty>();
            if (slots != null)
            {
                List<ChipFeaturePropertySlot> orderedSlots =
                    new List<ChipFeaturePropertySlot>(slots);
                orderedSlots.Sort((first, second) =>
                    (first?.Order ?? 0).CompareTo(second?.Order ?? 0));
                for (int index = 0; index < orderedSlots.Count; index++)
                {
                    ChipFeaturePropertySlot slot = orderedSlots[index];
                    if (slot?.Property == null
                        || !contributions.TryGetValue(
                            slot.Property,
                            out List<ChipFeatureValueContribution> values))
                    {
                        continue;
                    }

                    ChipInformationProperty finalProperty =
                        ChipFeatureContributionMergeService.Merge(
                            slot.Property,
                            values,
                            path,
                            diagnostics);
                    if (finalProperty == null)
                    {
                        continue;
                    }

                    properties.Add(new ChipInformationProperty(
                        slot.Property.defName,
                        null,
                        slot.Property.ValueKind,
                        finalProperty.Value,
                        slot.Property.UnitKey,
                        ResolveBaseProvenance(
                            slot.Property,
                            context,
                            values,
                            baseContributions)));
                }
            }

            return properties;
        }

        /// <summary>用声明配置重读同一来源，取得未应用构型前的基础值。</summary>
        private static Dictionary<ChipFeaturePropertyDef, List<ChipFeatureValueContribution>>
            ResolveBaseSources(
                IList<ChipFeatureSourceConfig> sources,
                ChipFeatureReadContext context,
                string path)
        {
            Dictionary<ChipFeaturePropertyDef, List<ChipFeatureValueContribution>> result =
                new Dictionary<ChipFeaturePropertyDef, List<ChipFeatureValueContribution>>();
            if (context?.DeclaredConfig == null
                || ReferenceEquals(context.DeclaredConfig, context.EffectiveConfig))
            {
                return result;
            }

            ChipFeatureReadContext baseContext = new ChipFeatureReadContext(
                context.Identity,
                context.DeclaredConfig,
                context.DeclaredConfig,
                context.Runtime,
                context.ActiveEntryIds);
            ResolveSources(
                sources,
                baseContext,
                path,
                result,
                new List<ChipInformationDiagnostic>());
            return result;
        }

        /// <summary>只保留声明配置来源值；有效值仍由最终属性 Value 单独提供。</summary>
        private static IReadOnlyList<ChipInformationProvenance> ResolveBaseProvenance(
            ChipFeaturePropertyDef property,
            ChipFeatureReadContext context,
            IEnumerable<ChipFeatureValueContribution> effectiveValues,
            IDictionary<ChipFeaturePropertyDef, List<ChipFeatureValueContribution>> baseContributions)
        {
            List<ChipInformationProvenance> result =
                new List<ChipInformationProvenance>();
            IEnumerable<ChipFeatureValueContribution> sourceValues = null;
            if (context?.DeclaredConfig == null)
            {
                return result;
            }

            if (ReferenceEquals(context.DeclaredConfig, context.EffectiveConfig))
            {
                sourceValues = effectiveValues;
            }
            else if (baseContributions != null)
            {
                baseContributions.TryGetValue(
                    property,
                    out List<ChipFeatureValueContribution> declaredValues);
                sourceValues = declaredValues;
            }

            if (sourceValues == null)
            {
                return result;
            }

            foreach (ChipFeatureValueContribution contribution in sourceValues)
            {
                if (contribution?.Provenance == null)
                {
                    continue;
                }

                for (int index = 0; index < contribution.Provenance.Count; index++)
                {
                    ChipInformationProvenance provenance = contribution.Provenance[index];
                    if (provenance != null
                        && provenance.ContributionKind == ChipInformationContributionKind.Base)
                    {
                        result.Add(provenance);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>调用来源 Worker 并收集类型化贡献。</summary>
        private static void ResolveSources(
            IList<ChipFeatureSourceConfig> sources,
            ChipFeatureReadContext context,
            string path,
            Dictionary<ChipFeaturePropertyDef, List<ChipFeatureValueContribution>> contributions,
            List<ChipInformationDiagnostic> diagnostics)
        {
            if (sources == null)
            {
                return;
            }

            for (int index = 0; index < sources.Count; index++)
            {
                ChipFeatureSourceConfig source = sources[index];
                if (!IsSourceActive(source, context?.ActiveEntryIds))
                {
                    continue;
                }

                if (!ChipFeatureSourceWorkerFactory.TryCreate(
                    source,
                    out IChipFeatureSourceWorker worker,
                    out string error))
                {
                    diagnostics.Add(new ChipInformationDiagnostic(
                        "FeatureWorkerCreateFailed",
                        path,
                        source?.Id,
                        ChipInformationDiagnosticSeverity.Error,
                        error));
                    continue;
                }

                try
                {
                    IReadOnlyList<ChipFeatureValueContribution> values =
                        worker.Resolve(source, context);
                    if (values == null)
                    {
                        continue;
                    }

                    for (int valueIndex = 0; valueIndex < values.Count; valueIndex++)
                    {
                        ChipFeatureValueContribution value = values[valueIndex];
                        if (value?.Property == null)
                        {
                            diagnostics.Add(new ChipInformationDiagnostic(
                                "FeatureContributionPropertyMissing",
                                path,
                                source.Id,
                                ChipInformationDiagnosticSeverity.Error,
                                "来源 Worker 返回了没有属性 Def 的贡献。"));
                            continue;
                        }

                        if (!contributions.TryGetValue(value.Property, out List<ChipFeatureValueContribution> list))
                        {
                            list = new List<ChipFeatureValueContribution>();
                            contributions[value.Property] = list;
                        }

                        list.Add(value);
                    }
                }
                catch (Exception exception)
                {
                    diagnostics.Add(new ChipInformationDiagnostic(
                        "FeatureWorkerResolveFailed",
                        path,
                        source.Id,
                        ChipInformationDiagnosticSeverity.Error,
                        "来源 Worker 解析异常：" + exception.GetType().FullName));
                }
            }
        }

        /// <summary>判断来源集合是否适用于当前形态或姿态。</summary>
        private static bool IsSourceSetActive(
            IList<ChipFeatureSourceConfig> sources,
            IReadOnlyList<string> activeEntryIds)
        {
            if (sources == null || sources.Count == 0)
            {
                return false;
            }

            if (activeEntryIds == null)
            {
                return true;
            }

            for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
            {
                ChipFeatureSourceConfig source = sources[sourceIndex];
                if (source == null)
                {
                    continue;
                }

                bool hasEntryReference = false;
                if (!string.IsNullOrWhiteSpace(source.EntryId))
                {
                    hasEntryReference = true;
                    if (IsEntryActive(source.EntryId, activeEntryIds))
                    {
                        return true;
                    }
                }

                if (source.EntryIds == null)
                {
                    if (!hasEntryReference)
                    {
                        return true;
                    }

                    continue;
                }

                for (int entryIndex = 0; entryIndex < source.EntryIds.Count; entryIndex++)
                {
                    string entryId = source.EntryIds[entryIndex];
                    if (string.IsNullOrWhiteSpace(entryId))
                    {
                        continue;
                    }

                    hasEntryReference = true;
                    if (IsEntryActive(entryId, activeEntryIds))
                    {
                        return true;
                    }
                }

                if (!hasEntryReference)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>判断单个来源是否适用于当前形态或姿态。</summary>
        private static bool IsSourceActive(
            ChipFeatureSourceConfig source,
            IReadOnlyList<string> activeEntryIds)
        {
            if (source == null)
            {
                return false;
            }

            if (activeEntryIds == null)
            {
                return true;
            }

            bool hasEntryReference = false;
            if (!string.IsNullOrWhiteSpace(source.EntryId))
            {
                hasEntryReference = true;
                if (IsEntryActive(source.EntryId, activeEntryIds))
                {
                    return true;
                }
            }

            if (source.EntryIds != null)
            {
                for (int index = 0; index < source.EntryIds.Count; index++)
                {
                    string entryId = source.EntryIds[index];
                    if (string.IsNullOrWhiteSpace(entryId))
                    {
                        continue;
                    }

                    hasEntryReference = true;
                    if (IsEntryActive(entryId, activeEntryIds))
                    {
                        return true;
                    }
                }
            }

            return !hasEntryReference;
        }

        /// <summary>判断功能自身、信息组、子组或子功能是否存在活动来源。</summary>
        private static bool IsDeclarationActive(
            ChipFeatureDeclaration declaration,
            IReadOnlyList<string> activeEntryIds)
        {
            if (declaration == null)
            {
                return false;
            }

            if (IsSourceSetActive(declaration.Sources, activeEntryIds))
            {
                return true;
            }

            if (declaration.Groups != null)
            {
                for (int index = 0; index < declaration.Groups.Count; index++)
                {
                    if (IsGroupDeclarationActive(
                        declaration.Groups[index],
                        activeEntryIds))
                    {
                        return true;
                    }
                }
            }

            if (declaration.Subgroups != null)
            {
                for (int index = 0; index < declaration.Subgroups.Count; index++)
                {
                    if (IsSourceSetActive(
                        declaration.Subgroups[index]?.Sources,
                        activeEntryIds))
                    {
                        return true;
                    }
                }
            }

            if (declaration.Children != null)
            {
                for (int index = 0; index < declaration.Children.Count; index++)
                {
                    if (IsDeclarationActive(declaration.Children[index], activeEntryIds))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>判断条目 Id 是否处于当前活动集合。</summary>
        private static bool IsEntryActive(
            string entryId,
            IReadOnlyList<string> activeEntryIds)
        {
            if (string.IsNullOrWhiteSpace(entryId)
                || activeEntryIds == null
                || activeEntryIds.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < activeEntryIds.Count; index++)
            {
                if (string.Equals(
                    activeEntryIds[index],
                    entryId,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
