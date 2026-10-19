using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 合并不同阶段产生的中性输入，并重分类阶段来源。
    /// </summary>
    public static class ChipInformationInputComposer
    {
        /// <summary>把多份阶段输入合并为一次解析输入。</summary>
        public static ChipInformationParseInput Combine(
            ChipInformationParseInput baseInput,
            ChipInformationParseInput overlayInput)
        {
            if (baseInput == null)
            {
                return overlayInput;
            }

            if (overlayInput == null)
            {
                return baseInput;
            }

            ChipInformationFacts facts = new ChipInformationFacts();
            AppendFacts(facts, baseInput.Facts);
            AppendFacts(facts, overlayInput.Facts);
            List<ChipInformationProvenance> sources =
                new List<ChipInformationProvenance>();
            AddRange(sources, baseInput.ProvenanceSources);
            AddRange(sources, overlayInput.ProvenanceSources);
            ChipInformationRuntimeState runtime = overlayInput.Runtime != null
                && overlayInput.Runtime.HasRuntimeState
                ? overlayInput.Runtime
                : baseInput.Runtime;
            return new ChipInformationParseInput(
                MergeIdentity(baseInput.Identity, overlayInput.Identity),
                facts,
                runtime,
                sources);
        }

        /// <summary>
        /// 合并阶段身份；运行时快照通常只有实例键，不能覆盖成品输入已有的职业与定义身份。
        /// </summary>
        private static ChipInformationIdentity MergeIdentity(
            ChipInformationIdentity baseIdentity,
            ChipInformationIdentity overlayIdentity)
        {
            if (baseIdentity == null)
            {
                return overlayIdentity;
            }

            if (overlayIdentity == null)
            {
                return baseIdentity;
            }

            return new ChipInformationIdentity(
                FirstNonEmpty(baseIdentity.InstanceKey, overlayIdentity.InstanceKey),
                FirstNonEmpty(overlayIdentity.DefinitionKey, baseIdentity.DefinitionKey),
                FirstNonEmpty(overlayIdentity.DisplayLabelKey, baseIdentity.DisplayLabelKey),
                FirstNonEmpty(overlayIdentity.SourceProfessionKey, baseIdentity.SourceProfessionKey));
        }

        /// <summary>选择阶段身份中已提供的稳定键。</summary>
        private static string FirstNonEmpty(string preferred, string fallback)
        {
            return !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback;
        }

        /// <summary>
        /// 将一份输入的字段来源重分类为阶段来源，值和字段结构保持不变。
        /// </summary>
        public static ChipInformationParseInput ReclassifySources(
            ChipInformationParseInput input,
            ChipInformationSourceKind sourceKind,
            ChipInformationContributionKind contributionKind,
            string sourceKeyPrefix)
        {
            if (input == null)
            {
                return null;
            }

            ChipInformationFacts facts = new ChipInformationFacts();
            CopyFactsWithSource(
                facts,
                input.Facts,
                sourceKind,
                contributionKind,
                sourceKeyPrefix);
            List<ChipInformationProvenance> sources = new List<ChipInformationProvenance>();
            if (input.ProvenanceSources != null)
            {
                for (int index = 0; index < input.ProvenanceSources.Count; index++)
                {
                    ChipInformationProvenance source = input.ProvenanceSources[index];
                    if (source == null)
                    {
                        continue;
                    }

                    sources.Add(new ChipInformationProvenance(
                        Prefix(sourceKeyPrefix, source.SourceKey),
                        sourceKind,
                        contributionKind,
                        source.Value,
                        source.Operation,
                        source.Applied));
                }
            }

            return new ChipInformationParseInput(
                input.Identity,
                facts,
                input.Runtime,
                sources);
        }

        /// <summary>复制所有事实集合。</summary>
        private static void AppendFacts(
            ChipInformationFacts target,
            ChipInformationFacts source)
        {
            if (target == null || source == null)
            {
                return;
            }

            AddRange(target.ProfileFields, source.ProfileFields);
            AddRange(target.LoadoutFields, source.LoadoutFields);
            AddRange(target.TrionFields, source.TrionFields);
            AddRange(target.ActivationConditions, source.ActivationConditions);
            AddRange(target.Expressions, source.Expressions);
            AddRange(target.StructuredBlocks, source.StructuredBlocks);
            AddRange(target.Contexts, source.Contexts);
            AddRange(target.Unknowns, source.Unknowns);
        }

        /// <summary>复制事实并重建字段来源。</summary>
        private static void CopyFactsWithSource(
            ChipInformationFacts target,
            ChipInformationFacts source,
            ChipInformationSourceKind sourceKind,
            ChipInformationContributionKind contributionKind,
            string sourceKeyPrefix)
        {
            if (target == null || source == null)
            {
                return;
            }

            CopyFieldsWithSource(target.ProfileFields, source.ProfileFields, sourceKind, contributionKind, sourceKeyPrefix);
            CopyFieldsWithSource(target.LoadoutFields, source.LoadoutFields, sourceKind, contributionKind, sourceKeyPrefix);
            CopyFieldsWithSource(target.TrionFields, source.TrionFields, sourceKind, contributionKind, sourceKeyPrefix);
            if (source.StructuredBlocks != null)
            {
                for (int index = 0; index < source.StructuredBlocks.Count; index++)
                {
                    ChipInformationFactBlock block = CopyFactBlockWithSource(
                        source.StructuredBlocks[index],
                        sourceKind,
                        contributionKind,
                        sourceKeyPrefix,
                        new HashSet<ChipInformationFactBlock>(),
                        0);
                    if (block != null)
                    {
                        target.StructuredBlocks.Add(block);
                    }
                }
            }
            AddRange(target.ActivationConditions, source.ActivationConditions);
            AddRange(target.Unknowns, source.Unknowns);
            if (source.Expressions != null)
            {
                for (int index = 0; index < source.Expressions.Count; index++)
                {
                    ChipInformationExpressionFact expression = CopyExpressionWithSource(
                        source.Expressions[index],
                        sourceKind,
                        contributionKind,
                        sourceKeyPrefix,
                        new HashSet<ChipInformationExpressionFact>(),
                        0);
                    if (expression != null)
                    {
                        target.Expressions.Add(expression);
                    }
                }
            }

            if (source.Contexts != null)
            {
                for (int index = 0; index < source.Contexts.Count; index++)
                {
                    ChipInformationContextFact context = source.Contexts[index];
                    if (context == null)
                    {
                        continue;
                    }

                    List<ChipInformationExpressionFact> expressions = new List<ChipInformationExpressionFact>();
                    if (context.Expressions != null)
                    {
                        for (int expressionIndex = 0; expressionIndex < context.Expressions.Count; expressionIndex++)
                        {
                            ChipInformationExpressionFact expression = CopyExpressionWithSource(
                                context.Expressions[expressionIndex],
                                sourceKind,
                                contributionKind,
                                sourceKeyPrefix,
                                new HashSet<ChipInformationExpressionFact>(),
                                0);
                            if (expression != null)
                            {
                                expressions.Add(expression);
                            }
                        }
                    }

                    target.Contexts.Add(new ChipInformationContextFact(
                        context.ContextKey,
                        context.LabelKey,
                        expressions,
                        context.ParentContextKey));
                }
            }
        }

        /// <summary>复制普通字段并重建来源。</summary>
        private static void CopyFieldsWithSource(
            ICollection<ChipInformationFactValue> target,
            IEnumerable<ChipInformationFactValue> source,
            ChipInformationSourceKind sourceKind,
            ChipInformationContributionKind contributionKind,
            string sourceKeyPrefix)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (ChipInformationFactValue fact in source)
            {
                if (fact == null)
                {
                    continue;
                }

                target.Add(new ChipInformationFactValue(
                    fact.FieldKey,
                    fact.LabelKey,
                    fact.ValueKind,
                    fact.Value,
                    fact.UnitKey,
                    CreateProvenance(
                        fact.Provenance,
                        sourceKind,
                        contributionKind,
                        Prefix(sourceKeyPrefix, fact.FieldKey))));
            }
        }

        /// <summary>复制表达事实并递归重建来源。</summary>
        private static ChipInformationExpressionFact CopyExpressionWithSource(
            ChipInformationExpressionFact source,
            ChipInformationSourceKind sourceKind,
            ChipInformationContributionKind contributionKind,
            string sourceKeyPrefix,
            ISet<ChipInformationExpressionFact> path,
            int depth)
        {
            if (source == null || path == null || depth > 32 || !path.Add(source))
            {
                return null;
            }

            List<ChipInformationFactValue> fields = new List<ChipInformationFactValue>();
            CopyFieldsWithSource(
                fields,
                source.Fields,
                sourceKind,
                contributionKind,
                Prefix(sourceKeyPrefix, source.ExpressionKey));
            List<ChipInformationExpressionFact> children = new List<ChipInformationExpressionFact>();
            if (source.Children != null)
            {
                for (int index = 0; index < source.Children.Count; index++)
                {
                    ChipInformationExpressionFact child = CopyExpressionWithSource(
                        source.Children[index],
                        sourceKind,
                        contributionKind,
                        Prefix(sourceKeyPrefix, source.ExpressionKey),
                        path,
                        depth + 1);
                    if (child != null)
                    {
                        children.Add(child);
                    }
                }
            }

            ChipInformationExpressionFact result = new ChipInformationExpressionFact(
                source.ExpressionKey,
                source.LabelKey,
                source.ChannelKey,
                source.SourceKey,
                sourceKind,
                source.WeaponModeKey,
                source.ModeKey,
                source.StanceKey,
                source.IsAvailable,
                fields,
                children,
                CopyFactBlocksWithSource(
                    source.Blocks,
                    sourceKind,
                    contributionKind,
                    Prefix(sourceKeyPrefix, source.ExpressionKey),
                    new HashSet<ChipInformationFactBlock>(),
                    0));
            path.Remove(source);
            return result;
        }

        /// <summary>递归重建表达下的结构化信息块来源。</summary>
        private static IReadOnlyList<ChipInformationFactBlock> CopyFactBlocksWithSource(
            IEnumerable<ChipInformationFactBlock> source,
            ChipInformationSourceKind sourceKind,
            ChipInformationContributionKind contributionKind,
            string sourceKeyPrefix,
            ISet<ChipInformationFactBlock> path,
            int depth)
        {
            List<ChipInformationFactBlock> result = new List<ChipInformationFactBlock>();
            if (source == null || depth > 32)
            {
                return result;
            }

            foreach (ChipInformationFactBlock block in source)
            {
                ChipInformationFactBlock copy = CopyFactBlockWithSource(
                    block,
                    sourceKind,
                    contributionKind,
                    sourceKeyPrefix,
                    path,
                    depth);
                if (copy != null)
                {
                    result.Add(copy);
                }
            }

            return result;
        }

        /// <summary>重建单个结构化信息块及其字段来源。</summary>
        private static ChipInformationFactBlock CopyFactBlockWithSource(
            ChipInformationFactBlock source,
            ChipInformationSourceKind sourceKind,
            ChipInformationContributionKind contributionKind,
            string sourceKeyPrefix,
            ISet<ChipInformationFactBlock> path,
            int depth)
        {
            if (source == null || path == null || depth > 32 || !path.Add(source))
            {
                return null;
            }

            List<ChipInformationFactValue> fields = new List<ChipInformationFactValue>();
            CopyFieldsWithSource(
                fields,
                source.Fields,
                sourceKind,
                contributionKind,
                Prefix(sourceKeyPrefix, source.BlockKey));
            IReadOnlyList<ChipInformationFactBlock> children = CopyFactBlocksWithSource(
                source.ChildBlocks,
                sourceKind,
                contributionKind,
                Prefix(sourceKeyPrefix, source.BlockKey),
                path,
                depth + 1);
            path.Remove(source);
            return new ChipInformationFactBlock(
                source.BlockKey,
                source.Kind,
                source.LabelKey,
                source.Order,
                fields,
                children);
        }

        /// <summary>构造重分类后的来源记录。</summary>
        private static IReadOnlyList<ChipInformationProvenance> CreateProvenance(
            IReadOnlyList<ChipInformationProvenance> original,
            ChipInformationSourceKind sourceKind,
            ChipInformationContributionKind contributionKind,
            string sourceKey)
        {
            List<ChipInformationProvenance> result = new List<ChipInformationProvenance>();
            if (original != null && original.Count > 0)
            {
                for (int index = 0; index < original.Count; index++)
                {
                    ChipInformationProvenance item = original[index];
                    if (item != null)
                    {
                        result.Add(new ChipInformationProvenance(
                            sourceKey,
                            sourceKind,
                            contributionKind,
                            item.Value,
                            item.Operation,
                            item.Applied));
                    }
                }
            }

            if (result.Count == 0)
            {
                result.Add(new ChipInformationProvenance(
                    sourceKey,
                    sourceKind,
                    contributionKind,
                    null,
                    null,
                    true));
            }

            return result;
        }

        /// <summary>拼接稳定来源键。</summary>
        private static string Prefix(string prefix, string value)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return value;
            }

            return string.IsNullOrWhiteSpace(value)
                ? prefix
                : prefix + "." + value;
        }

        /// <summary>复制只读集合。</summary>
        private static void AddRange<T>(ICollection<T> target, IEnumerable<T> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (T item in source)
            {
                if (item != null)
                {
                    target.Add(item);
                }
            }
        }
    }
}
