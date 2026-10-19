using System;
using System.Collections.Generic;
using BDP.Core.Combos;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using BDP.Core.Information;
using Verse;

namespace BDP.Content.Information.Combos
{
    /// <summary>
    /// 把组合技定义关系和已发布组合结果投影为芯片信息树上的中性引用。
    /// </summary>
    internal static class ComboChipInformationReferenceProjector
    {
        /// <summary>组合技信息扩展的稳定提供方键。</summary>
        private const string ProviderKey = "BDP.Combo";

        /// <summary>静态组合技定义引用的类型键。</summary>
        private const string DefinitionReferenceKindKey = "definition";

        /// <summary>运行时正式组合结果引用的类型键。</summary>
        private const string RuntimeReferenceKindKey = "runtime-result";

        /// <summary>
        /// 按来源功能模板、当前芯片身份和可选已发布快照创建组合技引用。
        /// </summary>
        internal static IReadOnlyList<ChipInformationExtensionReference> Create(
            ChipSourceReferenceSnapshot source,
            ChipDefinitionConfig config,
            string chipThingId,
            string activeModeKey,
            string activeStanceKey,
            ExpressionPublishedProjectionSnapshot publishedProjection)
        {
            List<ChipInformationExtensionReference> result =
                new List<ChipInformationExtensionReference>();
            AddDefinitionReferences(result, source, config);
            AddRuntimeReferences(
                result,
                chipThingId,
                activeModeKey,
                activeStanceKey,
                publishedProjection);
            return result.AsReadOnly();
        }

        /// <summary>按正式组合来源键反向查找静态定义引用。</summary>
        private static void AddDefinitionReferences(
            ICollection<ChipInformationExtensionReference> target,
            ChipSourceReferenceSnapshot source,
            ChipDefinitionConfig config)
        {
            if (target == null || source?.OrderedSourceKeys == null || source.OrderedSourceKeys.Count == 0)
            {
                return;
            }

            List<ComboDef> comboDefs = DefDatabase<ComboDef>.AllDefsListForReading;
            for (int comboIndex = 0; comboIndex < comboDefs.Count; comboIndex++)
            {
                ComboDef combo = comboDefs[comboIndex];
                if (combo == null || string.IsNullOrWhiteSpace(combo.defName))
                {
                    continue;
                }

                if (IsTheoreticalPairAllowed(combo, source, config, true))
                {
                    string contextKey;
                    if (TryResolveContextKey(combo.FirstSourceScope, out contextKey))
                    {
                        AddReference(
                            target,
                            DefinitionReferenceKindKey,
                            combo.defName,
                            contextKey,
                            "first-source");
                    }
                }

                if (IsTheoreticalPairAllowed(combo, source, config, false))
                {
                    string contextKey;
                    if (TryResolveContextKey(combo.SecondSourceScope, out contextKey))
                    {
                        AddReference(
                            target,
                            DefinitionReferenceKindKey,
                            combo.defName,
                            contextKey,
                            "second-source");
                    }
                }
            }
        }

        /// <summary>只从当前已发布快照查找确实引用该芯片的组合结果。</summary>
        private static void AddRuntimeReferences(
            ICollection<ChipInformationExtensionReference> target,
            string chipThingId,
            string activeModeKey,
            string activeStanceKey,
            ExpressionPublishedProjectionSnapshot publishedProjection)
        {
            if (target == null
                || string.IsNullOrWhiteSpace(chipThingId)
                || (!string.IsNullOrWhiteSpace(activeStanceKey)
                    && string.IsNullOrWhiteSpace(activeModeKey))
                || publishedProjection?.Results == null)
            {
                return;
            }

            string contextKey = ResolveActiveContextKey(activeModeKey, activeStanceKey);
            for (int resultIndex = 0; resultIndex < publishedProjection.Results.Count; resultIndex++)
            {
                ExpressionPublishedResultSnapshot comboResult =
                    publishedProjection.Results[resultIndex];
                if (comboResult == null
                    || !comboResult.IsPublished
                    || string.IsNullOrWhiteSpace(comboResult.ComboDefName)
                    || string.IsNullOrWhiteSpace(comboResult.ResultId)
                    || !ReferencesChip(comboResult, chipThingId, publishedProjection))
                {
                    continue;
                }

                AddReference(
                    target,
                    RuntimeReferenceKindKey,
                    comboResult.ResultId,
                    contextKey);
            }
        }

        /// <summary>判断正式组合结果的直接或复合来源是否包含指定芯片。</summary>
        private static bool ReferencesChip(
            ExpressionPublishedResultSnapshot comboResult,
            string chipThingId,
            ExpressionPublishedProjectionSnapshot publishedProjection)
        {
            if (MatchesChip(comboResult.SourceReference, chipThingId))
            {
                return true;
            }

            if (ContainsChip(
                comboResult.SourceResultIds,
                chipThingId,
                publishedProjection)
                || ContainsChip(
                    new[] { comboResult.MainSourceResultId, comboResult.SubSourceResultId },
                    chipThingId,
                    publishedProjection))
            {
                return true;
            }

            ExpressionPublishedCompositeReference compositeReference;
            return publishedProjection.TryGetCompositeReference(
                    comboResult.ResultId,
                    out compositeReference)
                && (ContainsChip(
                        compositeReference.SourceResultIds,
                        chipThingId,
                        publishedProjection)
                    || ContainsChip(
                        new[]
                        {
                            compositeReference.MainSourceResultId,
                            compositeReference.SubSourceResultId
                        },
                        chipThingId,
                        publishedProjection));
        }

        /// <summary>通过公开结果索引检查来源结果是否归属于目标芯片。</summary>
        private static bool ContainsChip(
            IReadOnlyList<string> sourceResultIds,
            string chipThingId,
            ExpressionPublishedProjectionSnapshot publishedProjection)
        {
            if (sourceResultIds == null)
            {
                return false;
            }

            for (int index = 0; index < sourceResultIds.Count; index++)
            {
                ExpressionPublishedResultSnapshot sourceResult;
                if (publishedProjection.TryGetResult(sourceResultIds[index], out sourceResult)
                    && MatchesChip(sourceResult.SourceReference, chipThingId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>比较公开来源引用中的芯片实例稳定身份。</summary>
        private static bool MatchesChip(
            ExpressionPublishedSourceReference sourceReference,
            string chipThingId)
        {
            return sourceReference != null
                && string.Equals(
                    sourceReference.ChipThingId,
                    chipThingId,
                    StringComparison.Ordinal);
        }

        /// <summary>当前成品按正式首来源身份筛查；另一侧先通过制造规则，再检查两侧组合准入。</summary>
        private static bool IsTheoreticalPairAllowed(ComboDef combo,
            ChipSourceReferenceSnapshot source, ChipDefinitionConfig config, bool firstSide)
        {
            if (!ComboDeclarationResolver.IsSourceAllowed(combo, config, source, firstSide)) return false;
            string partnerKey = firstSide ? combo.secondSourceKey : combo.firstSourceKey;
            if (string.Equals(source.OrderedSourceKeys[0], partnerKey, StringComparison.OrdinalIgnoreCase))
                return ComboDeclarationResolver.IsPairAllowed(combo, config, source, config, source);
            if (!ComboDeclaredSourceResolver.TryResolve(partnerKey, source.SourceProfessionKey, source.SourceVariantKey,
                out ChipDefinitionConfig partnerConfig, out ChipSourceReferenceSnapshot partner)) return false;
            return firstSide
                ? ComboDeclarationResolver.IsPairAllowed(combo, config, source, partnerConfig, partner)
                : ComboDeclarationResolver.IsPairAllowed(combo, partnerConfig, partner, config, source);
        }

        /// <summary>把组合来源范围转换为根、形态或姿态上下文键。</summary>
        private static bool TryResolveContextKey(
            ComboSourceScopeConfig scope,
            out string contextKey)
        {
            contextKey = null;
            if (scope == null)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(scope.StanceKey))
            {
                if (string.IsNullOrWhiteSpace(scope.ModeKey))
                {
                    return false;
                }

                contextKey = scope.ModeKey + "/" + scope.StanceKey;
                return true;
            }

            contextKey = string.IsNullOrWhiteSpace(scope.ModeKey)
                ? null
                : scope.ModeKey;
            return true;
        }

        /// <summary>把当前芯片形态和姿态转换为其实际上下文键。</summary>
        private static string ResolveActiveContextKey(
            string activeModeKey,
            string activeStanceKey)
        {
            if (string.IsNullOrWhiteSpace(activeModeKey))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(activeStanceKey)
                ? activeModeKey
                : activeModeKey + "/" + activeStanceKey;
        }

        /// <summary>追加唯一的提供方、类型、目标和上下文组合键。</summary>
        private static void AddReference(
            ICollection<ChipInformationExtensionReference> target,
            string referenceKindKey,
            string referenceKey,
            string contextKey,
            string relationRoleKey = null)
        {
            if (target == null || string.IsNullOrWhiteSpace(referenceKey))
            {
                return;
            }

            foreach (ChipInformationExtensionReference existing in target)
            {
                if (existing != null
                    && string.Equals(existing.ProviderKey, ProviderKey, StringComparison.Ordinal)
                    && string.Equals(existing.ReferenceKindKey, referenceKindKey, StringComparison.Ordinal)
                    && string.Equals(existing.ReferenceKey, referenceKey, StringComparison.Ordinal)
                    && string.Equals(existing.ContextKey ?? string.Empty, contextKey ?? string.Empty, StringComparison.Ordinal)
                    && string.Equals(existing.RelationRoleKey ?? string.Empty, relationRoleKey ?? string.Empty, StringComparison.Ordinal))
                {
                    return;
                }
            }

            target.Add(new ChipInformationExtensionReference(
                ProviderKey,
                referenceKindKey,
                referenceKey,
                contextKey,
                relationRoleKey));
        }
    }
}
