using System;
using System.Collections.Generic;
using BDP.Core.Chips;

namespace BDP.Core.Information
{
    /// <summary>
    /// 将中性芯片事实解析为统一对外信息模型。
    /// </summary>
    public sealed class ChipInformationParser
    {
        /// <summary>通用规则注册表。</summary>
        private readonly ChipInformationRuleRegistry registry;

        /// <summary>规则合并器。</summary>
        private readonly ChipInformationMergeService mergeService =
            new ChipInformationMergeService();

        /// <summary>创建信息解析器。</summary>
        public ChipInformationParser(ChipInformationRuleRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>解析一次中性事实输入。</summary>
        public ChipInformationModel Parse(ChipInformationParseInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return ParseInternal(input, true, null);
        }

        /// <summary>按统一请求解析一般信息和显式功能语义。</summary>
        public ChipInformationModel Parse(
            ChipInformationParseInput input,
            ChipInformationRequest request)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return ParseInternal(input, true, request);
        }

        /// <summary>递归解析一个输入及其上下文。</summary>
        private ChipInformationModel ParseInternal(
            ChipInformationParseInput input,
            bool includeContexts,
            ChipInformationRequest request)
        {
            ChipInformationRuleContext context = new ChipInformationRuleContext(input);
            List<ChipInformationBlockSchema> schemas =
                new List<ChipInformationBlockSchema>();
            List<ChipInformationContribution> contributions =
                new List<ChipInformationContribution>();
            List<ChipInformationDiagnostic> diagnostics =
                new List<ChipInformationDiagnostic>();
            AddUnknownDiagnostics(input.Facts, diagnostics);
            AddExpressionSafetyDiagnostics(input.Facts, diagnostics);

            for (int index = 0; index < registry.Rules.Count; index++)
            {
                IChipInformationRule rule = registry.Rules[index];
                if (rule == null)
                {
                    continue;
                }

                try
                {
                    if (!rule.CanApply(context))
                    {
                        continue;
                    }

                    AddRange(schemas, rule.DescribeSchema(context));
                    AddRange(contributions, rule.Resolve(context));
                }
                catch (Exception exception)
                {
                    diagnostics.Add(new ChipInformationDiagnostic(
                        "information.rule_failed",
                        null,
                        null,
                        ChipInformationDiagnosticSeverity.Error,
                        rule.RuleKey + ":" + exception.GetType().Name));
                }
            }

            // BlockKey（信息块稳定键）始终由规则声明并交给合并器统一组装。
            ChipInformationMergeResult merged = mergeService.Merge(
                schemas,
                contributions);
            AddRange(diagnostics, merged.Diagnostics);

            List<ChipInformationNode> contexts =
                includeContexts
                ? ParseContexts(input, diagnostics)
                    : new List<ChipInformationNode>();
            List<ChipInformationNode> features = new List<ChipInformationNode>();
            if (request != null)
            {
                ChipDefinitionConfig effectiveConfig = request.EffectiveConfig
                    ?? request.DeclaredConfig;
                if (effectiveConfig != null)
                {
                    AddRange(
                        diagnostics,
                        ChipFeatureDefinitionValidator.Validate(effectiveConfig));
                    ChipFeatureResolver resolver = new ChipFeatureResolver();
                    bool hasModeScopes = effectiveConfig.Expression?.Modes != null
                        && effectiveConfig.Expression.Modes.Count > 0;
                    if (hasModeScopes)
                    {
                        ChipFeatureContextProjectionResult projection =
                            new ChipFeatureContextProjector().Project(
                                request.DeclaredConfig,
                                effectiveConfig,
                                request.Identity,
                                request.Runtime,
                                effectiveConfig.Features,
                                resolver);
                        AddRange(diagnostics, projection.Diagnostics);
                        AddRange(features, projection.RootFeatures);
                        contexts = MergeFeatureContexts(contexts, projection.Contexts);
                    }
                    else
                    {
                        // 无形态树的芯片把整份功能声明作为唯一根级范围读取。
                        ChipFeatureReadContext readContext = new ChipFeatureReadContext(
                            request.Identity,
                            request.DeclaredConfig,
                            effectiveConfig,
                            request.Runtime,
                            null);
                        ChipFeatureResolutionResult resolution = resolver.Resolve(
                            effectiveConfig.Features,
                            readContext);
                        AddRange(diagnostics, resolution.Diagnostics);
                        AddRange(features, resolution.Features);
                    }
                }
            }

            List<ChipInformationExtensionReference> rootExtensionReferences =
                new List<ChipInformationExtensionReference>();
            if (request != null)
            {
                contexts = ProjectExtensionReferences(
                    contexts,
                    request.ExtensionReferences,
                    diagnostics,
                    out rootExtensionReferences);
            }
            contexts = NestContexts(contexts);

            ChipInformationNode root = ChipInformationNodeProjector.Project(
                input.Identity,
                merged.Nodes,
                features,
                contexts,
                rootExtensionReferences,
                request?.FeaturePropertySources);
            return new ChipInformationModel(root, diagnostics);
        }

        /// <summary>按稳定键合并一般上下文与功能上下文节点。</summary>
        private static List<ChipInformationNode> MergeFeatureContexts(
            IReadOnlyList<ChipInformationNode> existingContexts,
            IReadOnlyList<ChipInformationNode> featureContexts)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (existingContexts != null)
            {
                for (int index = 0; index < existingContexts.Count; index++)
                {
                    if (IsContext(existingContexts[index]))
                    {
                        result.Add(existingContexts[index]);
                    }
                }
            }

            if (featureContexts == null)
            {
                return result;
            }

            for (int featureIndex = 0; featureIndex < featureContexts.Count; featureIndex++)
            {
                ChipInformationNode featureContext = featureContexts[featureIndex];
                if (!IsContext(featureContext))
                {
                    continue;
                }

                int existingIndex = FindContextIndex(result, featureContext.Key);
                if (existingIndex < 0)
                {
                    result.Add(featureContext);
                    continue;
                }

                result[existingIndex] = MergeContextNode(result[existingIndex], featureContext);
            }

            return result;
        }

        /// <summary>按稳定上下文键查找可合并的既有节点。</summary>
        private static int FindContextIndex(
            IReadOnlyList<ChipInformationNode> contexts,
            string contextKey)
        {
            if (contexts == null || string.IsNullOrWhiteSpace(contextKey))
            {
                return -1;
            }

            for (int index = 0; index < contexts.Count; index++)
            {
                if (string.Equals(
                    contexts[index]?.Key,
                    contextKey,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>合并两个同键上下文，并递归合并其中重复的子上下文。</summary>
        private static ChipInformationNode MergeContextNode(
            ChipInformationNode existing,
            ChipInformationNode incoming)
        {
            List<ChipInformationNode> children = new List<ChipInformationNode>();
            AddRange(children, existing?.Children);
            if (incoming?.Children != null)
            {
                for (int index = 0; index < incoming.Children.Count; index++)
                {
                    ChipInformationNode child = incoming.Children[index];
                    int childIndex = IsContext(child)
                        ? FindContextIndex(children, child.Key)
                        : -1;
                    if (childIndex < 0)
                    {
                        children.Add(child);
                    }
                    else
                    {
                        children[childIndex] = MergeContextNode(children[childIndex], child);
                    }
                }
            }

            return new ChipInformationNode(
                existing.Key,
                existing.KindKey,
                existing.DefinitionKey,
                string.IsNullOrWhiteSpace(existing.LabelKey)
                    ? incoming.LabelKey
                    : existing.LabelKey,
                existing.Order,
                existing.Properties,
                children);
        }

        /// <summary>递归收集上下文节点，便于按稳定键挂接关系。</summary>
        private static void AppendContexts(
            IEnumerable<ChipInformationNode> source,
            ICollection<ChipInformationNode> target)
        {
            if (source == null || target == null)
            {
                return;
            }

            foreach (ChipInformationNode node in source)
            {
                if (!IsContext(node))
                {
                    continue;
                }

                target.Add(node);
                AppendContexts(node.Children, target);
            }
        }

        /// <summary>按上下文键递归追加已验证的关系节点。</summary>
        private static List<ChipInformationNode> AddContextReferences(
            IEnumerable<ChipInformationNode> source,
            IDictionary<string, List<ChipInformationExtensionReference>> referencesByContext)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (source == null)
            {
                return result;
            }

            foreach (ChipInformationNode node in source)
            {
                if (node == null)
                {
                    continue;
                }

                List<ChipInformationNode> children = AddContextReferences(
                    node.Children,
                    referencesByContext);
                List<ChipInformationExtensionReference> references;
                if (IsContext(node)
                    && referencesByContext != null
                    && referencesByContext.TryGetValue(node.Key, out references))
                {
                    for (int index = 0; index < references.Count; index++)
                    {
                        ChipInformationNode relation =
                            ChipInformationNodeProjector.CreateRelationNode(references[index]);
                        if (relation != null)
                        {
                            children.Add(relation);
                        }
                    }
                }

                result.Add(new ChipInformationNode(
                    node.Key,
                    node.KindKey,
                    node.DefinitionKey,
                    node.LabelKey,
                    node.Order,
                    node.Properties,
                    children));
            }

            return result;
        }

        /// <summary>检查节点是否为上下文。</summary>
        private static bool IsContext(ChipInformationNode node)
        {
            return node != null
                && string.Equals(node.KindKey, "context", StringComparison.Ordinal);
        }

        /// <summary>按输入事实里的父键一次性组装上下文树。</summary>
        private static List<ChipInformationNode> NestContexts(
            IEnumerable<ChipInformationNode> source)
        {
            List<ContextEntry> entries = new List<ContextEntry>();
            CollectContextEntries(source, null, entries);
            List<ChipInformationNode> roots = new List<ChipInformationNode>();
            HashSet<ContextEntry> built = new HashSet<ContextEntry>();
            for (int index = 0; index < entries.Count; index++)
            {
                ContextEntry entry = entries[index];
                if (string.IsNullOrWhiteSpace(entry.ParentKey)
                    || FindContextEntries(entries, entry.ParentKey).Count == 0)
                {
                    ChipInformationNode root = BuildContextNode(entry, entries, built, new HashSet<ContextEntry>());
                    if (root != null)
                    {
                        roots.Add(root);
                    }
                }
            }

            for (int index = 0; index < entries.Count; index++)
            {
                if (!built.Contains(entries[index]))
                {
                    ChipInformationNode root = BuildContextNode(
                        entries[index], entries, built, new HashSet<ContextEntry>());
                    if (root != null)
                    {
                        roots.Add(root);
                    }
                }
            }

            return roots;
        }

        /// <summary>收集上下文节点并继承树中已存在的父关系。</summary>
        private static void CollectContextEntries(
            IEnumerable<ChipInformationNode> source,
            string inheritedParentKey,
            ICollection<ContextEntry> target)
        {
            if (source == null || target == null)
            {
                return;
            }

            foreach (ChipInformationNode node in source)
            {
                if (!IsContext(node))
                {
                    continue;
                }

                string parentKey = ReadParentContextKey(node) ?? inheritedParentKey;
                target.Add(new ContextEntry(node, parentKey));
                CollectContextEntries(node.Children, node.Key, target);
            }
        }

        /// <summary>按父键构建上下文节点并移除组装期标记。</summary>
        private static ChipInformationNode BuildContextNode(
            ContextEntry entry,
            IReadOnlyList<ContextEntry> entries,
            ISet<ContextEntry> built,
            ISet<ContextEntry> path)
        {
            if (entry == null || !path.Add(entry) || built.Contains(entry))
            {
                return null;
            }

            List<ChipInformationProperty> properties = new List<ChipInformationProperty>();
            if (entry.Node.Properties != null)
            {
                for (int index = 0; index < entry.Node.Properties.Count; index++)
                {
                    ChipInformationProperty property = entry.Node.Properties[index];
                    if (!string.Equals(property?.Key, ChipInformationNodeProjector.ParentContextPropertyKey, StringComparison.Ordinal))
                    {
                        properties.Add(property);
                    }
                }
            }

            List<ChipInformationNode> children = new List<ChipInformationNode>();
            if (entry.Node.Children != null)
            {
                for (int index = 0; index < entry.Node.Children.Count; index++)
                {
                    if (!IsContext(entry.Node.Children[index]))
                    {
                        children.Add(entry.Node.Children[index]);
                    }
                }
            }

            List<ContextEntry> childEntries = FindContextEntries(entries, entry.Node.Key);
            for (int index = 0; index < childEntries.Count; index++)
            {
                ChipInformationNode child = BuildContextNode(
                    childEntries[index], entries, built, path);
                if (child != null)
                {
                    children.Add(child);
                }
            }

            path.Remove(entry);
            built.Add(entry);
            return new ChipInformationNode(
                entry.Node.Key,
                entry.Node.KindKey,
                entry.Node.DefinitionKey,
                entry.Node.LabelKey,
                entry.Node.Order,
                properties,
                children);
        }

        /// <summary>查找所有挂在指定上下文键下的子节点。</summary>
        private static List<ContextEntry> FindContextEntries(
            IReadOnlyList<ContextEntry> entries,
            string parentKey)
        {
            List<ContextEntry> result = new List<ContextEntry>();
            if (entries == null || string.IsNullOrWhiteSpace(parentKey))
            {
                return result;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                if (string.Equals(entries[index].ParentKey, parentKey, StringComparison.Ordinal))
                {
                    result.Add(entries[index]);
                }
            }

            return result;
        }

        /// <summary>读取仅供上下文组装使用的父键属性。</summary>
        private static string ReadParentContextKey(ChipInformationNode node)
        {
            if (node?.Properties == null)
            {
                return null;
            }

            for (int index = 0; index < node.Properties.Count; index++)
            {
                ChipInformationProperty property = node.Properties[index];
                if (string.Equals(property?.Key, ChipInformationNodeProjector.ParentContextPropertyKey, StringComparison.Ordinal))
                {
                    return property.Value?.Text;
                }
            }

            return null;
        }

        /// <summary>上下文组装过程中的节点与父键。</summary>
        private sealed class ContextEntry
        {
            internal ChipInformationNode Node { get; }
            internal string ParentKey { get; }

            internal ContextEntry(ChipInformationNode node, string parentKey)
            {
                Node = node;
                ParentKey = parentKey;
            }
        }

        /// <summary>按稳定上下文键把扩展引用放入根节点或对应上下文。</summary>
        private static List<ChipInformationNode> ProjectExtensionReferences(
            IReadOnlyList<ChipInformationNode> contexts,
            IReadOnlyList<ChipInformationExtensionReference> references,
            ICollection<ChipInformationDiagnostic> diagnostics,
            out List<ChipInformationExtensionReference> rootReferences)
        {
            rootReferences = new List<ChipInformationExtensionReference>();
            IReadOnlyList<ChipInformationNode> sourceContexts = contexts
                ?? new List<ChipInformationNode>();
            Dictionary<string, int> contextIndexes =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<ChipInformationNode> flattenedContexts = new List<ChipInformationNode>();
            AppendContexts(sourceContexts, flattenedContexts);
            Dictionary<string, List<ChipInformationExtensionReference>> referencesByContext =
                new Dictionary<string, List<ChipInformationExtensionReference>>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < flattenedContexts.Count; index++)
            {
                ChipInformationNode context = flattenedContexts[index];
                if (context == null || string.IsNullOrWhiteSpace(context.Key))
                {
                    continue;
                }

                if (contextIndexes.ContainsKey(context.Key))
                {
                    contextIndexes[context.Key] = -1;
                }
                else
                {
                    contextIndexes.Add(context.Key, index);
                }
            }

            if (references != null)
            {
                for (int index = 0; index < references.Count; index++)
                {
                    ChipInformationExtensionReference reference = references[index];
                    if (reference == null
                        || string.IsNullOrWhiteSpace(reference.ProviderKey)
                        || string.IsNullOrWhiteSpace(reference.ReferenceKindKey)
                        || string.IsNullOrWhiteSpace(reference.ReferenceKey))
                    {
                        AddExtensionDiagnostic(
                            diagnostics,
                            "information.extension_reference_invalid",
                            reference);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(reference.ContextKey))
                    {
                        rootReferences.Add(reference);
                        continue;
                    }

                    int contextIndex;
                    if (!contextIndexes.TryGetValue(reference.ContextKey, out contextIndex))
                    {
                        AddExtensionDiagnostic(
                            diagnostics,
                            "information.extension_context_missing",
                            reference);
                        continue;
                    }

                    if (contextIndex < 0)
                    {
                        AddExtensionDiagnostic(
                            diagnostics,
                            "information.extension_context_ambiguous",
                            reference);
                        continue;
                    }

                    List<ChipInformationExtensionReference> contextReferences;
                    if (!referencesByContext.TryGetValue(reference.ContextKey, out contextReferences))
                    {
                        contextReferences = new List<ChipInformationExtensionReference>();
                        referencesByContext.Add(reference.ContextKey, contextReferences);
                    }

                    contextReferences.Add(reference);
                }
            }

            return AddContextReferences(sourceContexts, referencesByContext);
        }

        /// <summary>记录无效或无法定位的扩展引用，不猜测其应归属的位置。</summary>
        private static void AddExtensionDiagnostic(
            ICollection<ChipInformationDiagnostic> diagnostics,
            string code,
            ChipInformationExtensionReference reference)
        {
            if (diagnostics == null)
            {
                return;
            }

            diagnostics.Add(new ChipInformationDiagnostic(
                code,
                null,
                null,
                ChipInformationDiagnosticSeverity.Warning,
                reference == null
                    ? string.Empty
                    : (reference.ProviderKey ?? string.Empty)
                        + ":" + (reference.ReferenceKindKey ?? string.Empty)
                        + ":" + (reference.ReferenceKey ?? string.Empty)
                        + "@" + (reference.ContextKey ?? string.Empty)));
        }

        /// <summary>在规则运行前检测表达事实的循环引用和过深嵌套。</summary>
        private static void AddExpressionSafetyDiagnostics(
            ChipInformationFacts facts,
            ICollection<ChipInformationDiagnostic> diagnostics)
        {
            if (facts?.Expressions == null || diagnostics == null)
            {
                return;
            }

            for (int index = 0; index < facts.Expressions.Count; index++)
            {
                InspectExpression(
                    facts.Expressions[index],
                    new HashSet<ChipInformationExpressionFact>(),
                    0,
                    diagnostics);
            }
        }

        /// <summary>沿当前路径检查一条表达事实树。</summary>
        private static void InspectExpression(
            ChipInformationExpressionFact expression,
            ISet<ChipInformationExpressionFact> path,
            int depth,
            ICollection<ChipInformationDiagnostic> diagnostics)
        {
            if (expression == null || path == null || diagnostics == null)
            {
                return;
            }

            if (depth > 32)
            {
                diagnostics.Add(new ChipInformationDiagnostic(
                    "information.expression_depth_exceeded",
                    null,
                    null,
                    ChipInformationDiagnosticSeverity.Warning,
                    expression.ExpressionKey));
                return;
            }

            if (!path.Add(expression))
            {
                diagnostics.Add(new ChipInformationDiagnostic(
                    "information.expression_cycle",
                    null,
                    null,
                    ChipInformationDiagnosticSeverity.Warning,
                    expression.ExpressionKey));
                return;
            }

            if (expression.Children != null)
            {
                for (int index = 0; index < expression.Children.Count; index++)
                {
                    InspectExpression(
                        expression.Children[index],
                        path,
                        depth + 1,
                        diagnostics);
                }
            }

            path.Remove(expression);
        }

        /// <summary>把未知输入隔离到诊断集合，不制造未知玩家字段。</summary>
        private static void AddUnknownDiagnostics(
            ChipInformationFacts facts,
            ICollection<ChipInformationDiagnostic> diagnostics)
        {
            if (facts?.Unknowns == null || diagnostics == null)
            {
                return;
            }

            for (int index = 0; index < facts.Unknowns.Count; index++)
            {
                ChipInformationUnknownFact unknown = facts.Unknowns[index];
                if (unknown == null)
                {
                    continue;
                }

                diagnostics.Add(new ChipInformationDiagnostic(
                    unknown.Code,
                    null,
                    null,
                    ChipInformationDiagnosticSeverity.Warning,
                    unknown.SourceKey + ":" + unknown.Detail));
            }
        }

        /// <summary>递归解析输入中的上下文。</summary>
        private List<ChipInformationNode> ParseContexts(
            ChipInformationParseInput input,
            ICollection<ChipInformationDiagnostic> diagnostics)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (input.Facts == null || input.Facts.Contexts == null)
            {
                return result;
            }

            for (int index = 0; index < input.Facts.Contexts.Count; index++)
            {
                ChipInformationContextFact fact = input.Facts.Contexts[index];
                if (fact == null || string.IsNullOrWhiteSpace(fact.ContextKey))
                {
                    continue;
                }

                ChipInformationFacts nestedFacts = new ChipInformationFacts();
                if (fact.Expressions != null)
                {
                    for (int expressionIndex = 0;
                        expressionIndex < fact.Expressions.Count;
                        expressionIndex++)
                    {
                        if (fact.Expressions[expressionIndex] != null)
                        {
                            nestedFacts.Expressions.Add(fact.Expressions[expressionIndex]);
                        }
                    }
                }

                ChipInformationParseInput nestedInput = new ChipInformationParseInput(
                    input.Identity,
                    nestedFacts,
                    input.Runtime,
                    input.ProvenanceSources);
                ChipInformationModel nested = ParseInternal(nestedInput, false, null);
                AddRange(diagnostics, nested.Diagnostics);
                List<ChipInformationProperty> properties = new List<ChipInformationProperty>();
                if (!string.IsNullOrWhiteSpace(fact.ParentContextKey))
                {
                    properties.Add(new ChipInformationProperty(
                        ChipInformationNodeProjector.ParentContextPropertyKey,
                        null,
                        ChipInformationValueKind.Text,
                        ChipInformationValue.FromText(fact.ParentContextKey),
                        null,
                        null));
                }

                result.Add(new ChipInformationNode(
                    fact.ContextKey,
                    "context",
                    null,
                    fact.LabelKey,
                    index,
                    properties,
                    nested.Root?.Children));
            }

            return result;
        }

        /// <summary>安全追加只读集合。</summary>
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
