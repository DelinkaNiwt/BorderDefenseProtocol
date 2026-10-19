using System;
using System.Collections.Generic;
using BDP.Core.Chips;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片功能声明的结构校验器。
    /// </summary>
    public static class ChipFeatureDefinitionValidator
    {
        /// <summary>作者侧功能树允许的最大深度，根节点深度为 0。</summary>
        public const int MaximumDepth = 2;

        /// <summary>
        /// 校验一份芯片配置的功能声明并返回可定位诊断。
        /// </summary>
        public static IReadOnlyList<ChipInformationDiagnostic> Validate(
            ChipDefinitionConfig config,
            int maximumDepth = MaximumDepth)
        {
            List<ChipInformationDiagnostic> diagnostics =
                new List<ChipInformationDiagnostic>();
            if (config == null || config.Features == null)
            {
                return diagnostics.AsReadOnly();
            }

            HashSet<string> featureIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < config.Features.Count; index++)
            {
                ValidateDeclaration(
                    config,
                    config.Features[index],
                    null,
                    0,
                    maximumDepth,
                    featureIds,
                    diagnostics);
            }

            return diagnostics.AsReadOnly();
        }

        /// <summary>递归校验一项功能声明。</summary>
        private static void ValidateDeclaration(
            ChipDefinitionConfig config,
            ChipFeatureDeclaration declaration,
            string parentPath,
            int depth,
            int maximumDepth,
            HashSet<string> featureIds,
            List<ChipInformationDiagnostic> diagnostics)
        {
            string path = BuildPath(parentPath, declaration?.Id);
            if (declaration == null)
            {
                AddError(diagnostics, "FeatureDeclarationNull", path, null, "功能声明为空。");
                return;
            }

            if (string.IsNullOrWhiteSpace(declaration.Id))
            {
                AddError(diagnostics, "FeatureIdMissing", path, null, "功能实例 Id 为空。");
            }
            else if (!featureIds.Add(path))
            {
                AddError(diagnostics, "FeatureIdDuplicate", path, null, "功能实例 Id 在当前配置范围内重复。");
            }

            if (declaration.Feature == null)
            {
                AddError(diagnostics, "FeatureDefMissing", path, null, "功能 FeatureDef 缺失。");
            }

            if (depth > maximumDepth)
            {
                AddError(diagnostics, "FeatureDepthExceeded", path, null, "功能子树超过允许深度。");
                return;
            }

            ValidateSources(config, declaration.Sources, path, diagnostics);
            ValidateGroups(config, declaration, path, diagnostics);
            ValidateSubgroups(
                config,
                declaration.Feature?.Subgroups,
                declaration.Subgroups,
                path + "/subgroups/",
                "FeatureSubgroupNotAccepted",
                "功能语义未声明允许该子组。",
                diagnostics);

            if (declaration.Children == null)
            {
                return;
            }

            for (int index = 0; index < declaration.Children.Count; index++)
            {
                ValidateDeclaration(
                    config,
                    declaration.Children[index],
                    path,
                    depth + 1,
                    maximumDepth,
                    featureIds,
                    diagnostics);
            }
        }

        /// <summary>校验功能内信息组的身份、允许范围及来源。</summary>
        private static void ValidateGroups(
            ChipDefinitionConfig config,
            ChipFeatureDeclaration declaration,
            string featurePath,
            List<ChipInformationDiagnostic> diagnostics)
        {
            if (declaration.Groups == null)
            {
                return;
            }

            HashSet<string> groupIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<ChipFeatureGroupDef> allowedGroups =
                new HashSet<ChipFeatureGroupDef>();
            if (declaration.Feature?.Groups != null)
            {
                for (int slotIndex = 0; slotIndex < declaration.Feature.Groups.Count; slotIndex++)
                {
                    ChipFeatureGroupDef allowedGroup = declaration.Feature.Groups[slotIndex]?.Group;
                    if (allowedGroup != null)
                    {
                        allowedGroups.Add(allowedGroup);
                    }
                }
            }

            for (int index = 0; index < declaration.Groups.Count; index++)
            {
                ChipFeatureGroupDeclaration group = declaration.Groups[index];
                if (group == null)
                {
                    AddError(
                        diagnostics,
                        "FeatureGroupDeclarationNull",
                        featurePath + "/groups",
                        null,
                        "功能信息组声明为空。");
                    continue;
                }

                string groupPath = featurePath + "/groups/" + (group.Id ?? string.Empty);
                if (string.IsNullOrWhiteSpace(group.Id))
                {
                    AddError(
                        diagnostics,
                        "GroupIdMissing",
                        groupPath,
                        null,
                        "信息组实例 Id 为空。");
                }
                else if (!groupIds.Add(group.Id))
                {
                    AddError(
                        diagnostics,
                        "GroupIdDuplicate",
                        groupPath,
                        null,
                        "同一功能内的信息组实例 Id 重复。");
                }

                if (group.Group == null)
                {
                    AddError(
                        diagnostics,
                        "GroupDefMissing",
                        groupPath,
                        null,
                        "信息组 GroupDef 缺失。");
                }
                else if (!allowedGroups.Contains(group.Group))
                {
                    AddError(
                        diagnostics,
                        "GroupNotAccepted",
                        groupPath,
                        group.Group.defName,
                        "功能语义未声明允许该信息组。");
                }

                ValidateSources(config, group.Sources, groupPath, diagnostics);
                ValidateSubgroups(
                    config,
                    group.Group?.Subgroups,
                    group.Subgroups,
                    groupPath + "/groups/",
                    "SubgroupNotAccepted",
                    "父信息组未声明允许该子组。",
                    diagnostics);
            }
        }

        /// <summary>校验一层子组的身份、允许范围、嵌套边界及来源。</summary>
        private static void ValidateSubgroups(
            ChipDefinitionConfig config,
            IList<ChipFeatureGroupSlot> allowedSlots,
            IList<ChipFeatureSubgroupDeclaration> subgroups,
            string subgroupPathPrefix,
            string notAcceptedCode,
            string notAcceptedMessage,
            List<ChipInformationDiagnostic> diagnostics)
        {
            if (subgroups == null)
            {
                return;
            }

            HashSet<string> subgroupIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<ChipFeatureGroupDef> allowedSubgroups =
                new HashSet<ChipFeatureGroupDef>();
            if (allowedSlots != null)
            {
                for (int slotIndex = 0; slotIndex < allowedSlots.Count; slotIndex++)
                {
                    ChipFeatureGroupDef allowedGroup =
                        allowedSlots[slotIndex]?.Group;
                    if (allowedGroup != null)
                    {
                        allowedSubgroups.Add(allowedGroup);
                    }
                }
            }

            for (int index = 0; index < subgroups.Count; index++)
            {
                ChipFeatureSubgroupDeclaration subgroup = subgroups[index];
                if (subgroup == null)
                {
                    AddError(
                        diagnostics,
                        "FeatureSubgroupDeclarationNull",
                        subgroupPathPrefix,
                        null,
                        "子组声明为空。");
                    continue;
                }

                string subgroupPath = subgroupPathPrefix + (subgroup.Id ?? string.Empty);
                if (string.IsNullOrWhiteSpace(subgroup.Id))
                {
                    AddError(
                        diagnostics,
                        "SubgroupIdMissing",
                        subgroupPath,
                        null,
                        "子组实例 Id 为空。");
                }
                else if (!subgroupIds.Add(subgroup.Id))
                {
                    AddError(
                        diagnostics,
                        "SubgroupIdDuplicate",
                        subgroupPath,
                        null,
                        "同一所属功能或信息组内的子组实例 Id 重复。");
                }

                if (subgroup.Group == null)
                {
                    AddError(
                        diagnostics,
                        "SubgroupDefMissing",
                        subgroupPath,
                        null,
                        "子组 GroupDef 缺失。");
                }
                else
                {
                    if (!allowedSubgroups.Contains(subgroup.Group))
                    {
                        AddError(
                            diagnostics,
                            notAcceptedCode,
                            subgroupPath,
                            subgroup.Group.defName,
                            notAcceptedMessage);
                    }

                    if (subgroup.Group.Subgroups != null
                        && subgroup.Group.Subgroups.Count > 0)
                    {
                        AddError(
                            diagnostics,
                            "SubgroupNestingNotSupported",
                            subgroupPath,
                            subgroup.Group.defName,
                            "子组不得继续声明子组；允许的结构为功能、信息组和子组三层。");
                    }
                }

                ValidateSources(config, subgroup.Sources, subgroupPath, diagnostics);
            }
        }

        /// <summary>校验来源 Id、WorkerClass 和表达条目引用。</summary>
        private static void ValidateSources(
            ChipDefinitionConfig config,
            IList<ChipFeatureSourceConfig> sources,
            string path,
            List<ChipInformationDiagnostic> diagnostics)
        {
            if (sources == null)
            {
                return;
            }

            HashSet<string> sourceIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> entryIds = CollectExpressionEntryIds(config);
            for (int index = 0; index < sources.Count; index++)
            {
                ChipFeatureSourceConfig source = sources[index];
                if (source == null)
                {
                    AddError(diagnostics, "FeatureSourceNull", path, null, "功能来源为空。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(source.Id))
                {
                    AddError(diagnostics, "FeatureSourceIdMissing", path, null, "功能来源 Id 为空。");
                }
                else if (!sourceIds.Add(source.Id))
                {
                    AddError(diagnostics, "FeatureSourceIdDuplicate", path, source.Id, "功能来源 Id 重复。");
                }

                if (source.WorkerClass == null)
                {
                    AddError(diagnostics, "FeatureWorkerMissing", path, source.Id, "功能来源 WorkerClass 为空。");
                }

                bool hasEntryReference = !string.IsNullOrWhiteSpace(source.EntryId);
                if (hasEntryReference)
                {
                    ValidateEntryReference(
                        source.EntryId,
                        source.Id,
                        entryIds,
                        path,
                        diagnostics);
                }

                if (source.EntryIds != null)
                {
                    for (int entryIndex = 0; entryIndex < source.EntryIds.Count; entryIndex++)
                    {
                        string entryId = source.EntryIds[entryIndex];
                        if (string.IsNullOrWhiteSpace(entryId))
                        {
                            continue;
                        }

                        hasEntryReference = true;
                        ValidateEntryReference(
                            entryId,
                            source.Id,
                            entryIds,
                            path,
                            diagnostics);
                    }
                }

                if (source.RequiresExpressionEntryReference && !hasEntryReference)
                {
                    ValidateEntryReference(
                        null,
                        source.Id,
                        entryIds,
                        path,
                        diagnostics);
                }
            }
        }

        /// <summary>校验一个表达条目引用。</summary>
        private static void ValidateEntryReference(
            string entryId,
            string sourceId,
            HashSet<string> entryIds,
            string path,
            List<ChipInformationDiagnostic> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(entryId) || !entryIds.Contains(entryId))
            {
                AddError(
                    diagnostics,
                    "FeatureEntryReferenceMissing",
                    path,
                    sourceId,
                    "功能来源引用的表达条目不存在：" + entryId);
            }
        }

        /// <summary>收集当前配置中所有表达条目的稳定 Id。</summary>
        private static HashSet<string> CollectExpressionEntryIds(ChipDefinitionConfig config)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            if (config?.Expression?.Entries == null)
            {
                return result;
            }

            for (int index = 0; index < config.Expression.Entries.Count; index++)
            {
                string id = config.Expression.Entries[index]?.Id;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    result.Add(id);
                }
            }

            return result;
        }

        /// <summary>组合父路径和当前功能 Id。</summary>
        private static string BuildPath(string parentPath, string id)
        {
            if (string.IsNullOrWhiteSpace(parentPath))
            {
                return id ?? string.Empty;
            }

            return parentPath + "/" + (id ?? string.Empty);
        }

        /// <summary>追加一条错误诊断。</summary>
        private static void AddError(
            List<ChipInformationDiagnostic> diagnostics,
            string code,
            string blockKey,
            string fieldKey,
            string detail)
        {
            diagnostics.Add(new ChipInformationDiagnostic(
                code,
                blockKey,
                fieldKey,
                ChipInformationDiagnosticSeverity.Error,
                detail));
        }
    }
}
