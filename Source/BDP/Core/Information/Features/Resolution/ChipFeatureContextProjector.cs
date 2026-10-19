using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Expressions;

namespace BDP.Core.Information
{
    /// <summary>
    /// 按表达条目归属将每项功能声明唯一投影到根、形态或姿态范围。
    /// </summary>
    public sealed class ChipFeatureContextProjector
    {
        /// <summary>投影一份芯片配置的唯一功能树与形态/姿态上下文。</summary>
        public ChipFeatureContextProjectionResult Project(
            ChipDefinitionConfig config,
            ChipInformationIdentity identity,
            ChipInformationRuntimeState runtime,
            IEnumerable<ChipFeatureDeclaration> declarations,
            ChipFeatureResolver resolver)
        {
            return Project(
                config,
                config,
                identity,
                runtime,
                declarations,
                resolver);
        }

        /// <summary>按声明配置和有效配置分别建立功能范围与来源读取上下文。</summary>
        public ChipFeatureContextProjectionResult Project(
            ChipDefinitionConfig declaredConfig,
            ChipDefinitionConfig effectiveConfig,
            ChipInformationIdentity identity,
            ChipInformationRuntimeState runtime,
            IEnumerable<ChipFeatureDeclaration> declarations,
            ChipFeatureResolver resolver)
        {
            List<ChipInformationNode> rootFeatures =
                new List<ChipInformationNode>();
            List<ChipInformationNode> contexts =
                new List<ChipInformationNode>();
            List<ChipInformationDiagnostic> diagnostics =
                new List<ChipInformationDiagnostic>();
            if (effectiveConfig?.Expression?.Modes == null || resolver == null)
            {
                return new ChipFeatureContextProjectionResult(
                    rootFeatures,
                    contexts,
                    diagnostics);
            }

            FeatureScope rootScope = new FeatureScope(
                null,
                null,
                0,
                null,
                true);
            List<FeatureScope> scopes = new List<FeatureScope>();
            Dictionary<string, FeatureScope> entryOwners =
                new Dictionary<string, FeatureScope>(StringComparer.OrdinalIgnoreCase);
            // 这里建立展示归属；姿态不继承形态条目，运行时合约仍按原规则计算有效条目闭包。
            BuildScopes(
                effectiveConfig.Expression.Modes,
                rootScope,
                scopes,
                entryOwners);

            if (declarations != null)
            {
                foreach (ChipFeatureDeclaration declaration in declarations)
                {
                    if (declaration == null)
                    {
                        continue;
                    }

                    List<string> entryIds = CollectEntryIds(declaration);
                    FeatureScope owner = FindCommonScope(
                        entryIds,
                        entryOwners,
                        rootScope);
                    ChipFeatureReadContext readContext = new ChipFeatureReadContext(
                        identity,
                        declaredConfig,
                        effectiveConfig,
                        runtime,
                        entryIds);
                    ChipFeatureResolutionResult resolution = resolver.Resolve(
                        new[] { declaration },
                        readContext);
                    Append(diagnostics, resolution.Diagnostics);
                    if (owner.IsRoot)
                    {
                        AppendRange(rootFeatures, resolution.Features);
                    }
                    else
                    {
                        AppendRange(owner.Features, resolution.Features);
                    }
                }
            }

            for (int scopeIndex = 0; scopeIndex < scopes.Count; scopeIndex++)
            {
                FeatureScope scope = scopes[scopeIndex];
                List<ChipInformationProperty> properties =
                    new List<ChipInformationProperty>();
                if (scope.Parent != null && !scope.Parent.IsRoot)
                {
                    properties.Add(new ChipInformationProperty(
                        ChipInformationNodeProjector.ParentContextPropertyKey,
                        null,
                        ChipInformationValueKind.Text,
                        ChipInformationValue.FromText(scope.Parent.Key),
                        null,
                        null));
                }

                contexts.Add(new ChipInformationNode(
                    scope.Key,
                    "context",
                    null,
                    scope.LabelKey,
                    scope.Order,
                    properties,
                    scope.Features));
            }

            return new ChipFeatureContextProjectionResult(
                rootFeatures,
                contexts,
                diagnostics);
        }

        /// <summary>建立根下的形态与姿态范围，并记录每个条目的唯一展示归属。</summary>
        private static void BuildScopes(
            IList<ChipExpressionModeConfig> modes,
            FeatureScope rootScope,
            List<FeatureScope> scopes,
            IDictionary<string, FeatureScope> entryOwners)
        {
            if (modes == null)
            {
                return;
            }

            for (int modeIndex = 0; modeIndex < modes.Count; modeIndex++)
            {
                ChipExpressionModeConfig mode = modes[modeIndex];
                if (mode == null || string.IsNullOrWhiteSpace(mode.ModeKey))
                {
                    continue;
                }

                FeatureScope modeScope = new FeatureScope(
                    mode.ModeKey,
                    mode.DisplayLabelKey,
                    modeIndex,
                    rootScope,
                    false);
                scopes.Add(modeScope);
                RegisterEntryOwners(entryOwners, mode.ActiveEntryIds, modeScope);

                if (mode.Stances == null)
                {
                    continue;
                }

                for (int stanceIndex = 0; stanceIndex < mode.Stances.Count; stanceIndex++)
                {
                    ChipExpressionStanceConfig stance = mode.Stances[stanceIndex];
                    if (stance == null || string.IsNullOrWhiteSpace(stance.StanceKey))
                    {
                        continue;
                    }

                    FeatureScope stanceScope = new FeatureScope(
                        mode.ModeKey + "/" + stance.StanceKey,
                        stance.DisplayLabelKey,
                        stanceIndex,
                        modeScope,
                        false);
                    scopes.Add(stanceScope);
                    RegisterEntryOwners(entryOwners, stance.ActiveEntryIds, stanceScope);
                }
            }
        }

        /// <summary>递归收集功能、信息组、子组和子功能来源中的表达条目 Id。</summary>
        private static List<string> CollectEntryIds(ChipFeatureDeclaration declaration)
        {
            List<string> result = new List<string>();
            CollectEntryIds(declaration, result, 0);
            return result;
        }

        /// <summary>在有限功能深度内收集一棵功能声明树的所有来源条目。</summary>
        private static void CollectEntryIds(
            ChipFeatureDeclaration declaration,
            List<string> target,
            int depth)
        {
            if (declaration == null
                || target == null
                || depth > ChipFeatureDefinitionValidator.MaximumDepth)
            {
                return;
            }

            AppendSourceEntryIds(target, declaration.Sources);
            if (declaration.Groups != null)
            {
                for (int groupIndex = 0; groupIndex < declaration.Groups.Count; groupIndex++)
                {
                    ChipFeatureGroupDeclaration group = declaration.Groups[groupIndex];
                    if (group == null)
                    {
                        continue;
                    }

                    AppendSourceEntryIds(target, group.Sources);
                    CollectEntryIds(group.Subgroups, target);
                }
            }

            CollectEntryIds(declaration.Subgroups, target);
            if (declaration.Children != null)
            {
                for (int childIndex = 0; childIndex < declaration.Children.Count; childIndex++)
                {
                    CollectEntryIds(declaration.Children[childIndex], target, depth + 1);
                }
            }
        }

        /// <summary>收集一层子组声明中的来源条目。</summary>
        private static void CollectEntryIds(
            IList<ChipFeatureSubgroupDeclaration> subgroups,
            List<string> target)
        {
            if (subgroups == null)
            {
                return;
            }

            for (int subgroupIndex = 0; subgroupIndex < subgroups.Count; subgroupIndex++)
            {
                AppendSourceEntryIds(target, subgroups[subgroupIndex]?.Sources);
            }
        }

        /// <summary>按来源配置读取单个或多个表达条目 Id。</summary>
        private static void AppendSourceEntryIds(
            List<string> target,
            IList<ChipFeatureSourceConfig> sources)
        {
            if (target == null || sources == null)
            {
                return;
            }

            for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
            {
                ChipFeatureSourceConfig source = sources[sourceIndex];
                if (source == null)
                {
                    continue;
                }

                AppendUnique(target, source.EntryId);
                AppendUnique(target, source.EntryIds);
            }
        }

        /// <summary>按所有来源条目范围计算其最近共同父级。</summary>
        private static FeatureScope FindCommonScope(
            IReadOnlyList<string> entryIds,
            IDictionary<string, FeatureScope> entryOwners,
            FeatureScope rootScope)
        {
            FeatureScope commonScope = null;
            if (entryIds != null && entryOwners != null)
            {
                for (int entryIndex = 0; entryIndex < entryIds.Count; entryIndex++)
                {
                    FeatureScope owner;
                    if (!entryOwners.TryGetValue(entryIds[entryIndex], out owner))
                    {
                        continue;
                    }

                    commonScope = commonScope == null
                        ? owner
                        : FindLowestCommonAncestor(commonScope, owner, rootScope);
                }
            }

            return commonScope ?? rootScope;
        }

        /// <summary>取得两个范围在树中的最近共同祖先。</summary>
        private static FeatureScope FindLowestCommonAncestor(
            FeatureScope first,
            FeatureScope second,
            FeatureScope rootScope)
        {
            for (FeatureScope firstAncestor = first;
                firstAncestor != null;
                firstAncestor = firstAncestor.Parent)
            {
                for (FeatureScope secondAncestor = second;
                    secondAncestor != null;
                    secondAncestor = secondAncestor.Parent)
                {
                    if (ReferenceEquals(firstAncestor, secondAncestor))
                    {
                        return firstAncestor;
                    }
                }
            }

            return rootScope;
        }

        /// <summary>登记条目 Id 的范围所有者，重复条目由结构校验报告。</summary>
        private static void RegisterEntryOwners(
            IDictionary<string, FeatureScope> entryOwners,
            IEnumerable<string> entryIds,
            FeatureScope owner)
        {
            if (entryOwners == null || entryIds == null || owner == null)
            {
                return;
            }

            foreach (string entryId in entryIds)
            {
                if (!string.IsNullOrWhiteSpace(entryId)
                    && !entryOwners.ContainsKey(entryId))
                {
                    entryOwners.Add(entryId, owner);
                }
            }
        }

        /// <summary>按首次出现顺序追加尚未登记的条目 Id。</summary>
        private static void AppendUnique(List<string> target, string entryId)
        {
            if (target != null
                && !string.IsNullOrWhiteSpace(entryId)
                && !ContainsId(target, entryId))
            {
                target.Add(entryId);
            }
        }

        /// <summary>按首次出现顺序追加尚未登记的条目 Id 集合。</summary>
        private static void AppendUnique(List<string> target, IEnumerable<string> entryIds)
        {
            if (entryIds == null)
            {
                return;
            }

            foreach (string entryId in entryIds)
            {
                AppendUnique(target, entryId);
            }
        }

        /// <summary>以不区分大小写的稳定 Id 比较判断集合中是否已有条目。</summary>
        private static bool ContainsId(IEnumerable<string> values, string expected)
        {
            if (values == null || string.IsNullOrWhiteSpace(expected))
            {
                return false;
            }

            foreach (string value in values)
            {
                if (string.Equals(value, expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>追加已解析的功能节点。</summary>
        private static void AppendRange(
            ICollection<ChipInformationNode> target,
            IEnumerable<ChipInformationNode> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (ChipInformationNode node in source)
            {
                if (node != null)
                {
                    target.Add(node);
                }
            }
        }

        /// <summary>追加上下文解析诊断。</summary>
        private static void Append(
            List<ChipInformationDiagnostic> target,
            IReadOnlyList<ChipInformationDiagnostic> source)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                if (source[index] != null)
                {
                    target.Add(source[index]);
                }
            }
        }

        /// <summary>表示树中一个功能唯一归属范围。</summary>
        private sealed class FeatureScope
        {
            /// <summary>上下文树中的稳定键；根范围为空。</summary>
            internal readonly string Key;

            /// <summary>面向玩家的范围名称语言键。</summary>
            internal readonly string LabelKey;

            /// <summary>范围在同级中的稳定顺序。</summary>
            internal readonly int Order;

            /// <summary>当前范围的直接父级。</summary>
            internal readonly FeatureScope Parent;

            /// <summary>当前范围是否代表芯片根节点。</summary>
            internal readonly bool IsRoot;

            /// <summary>唯一归属于当前范围的已解析功能。</summary>
            internal readonly List<ChipInformationNode> Features =
                new List<ChipInformationNode>();

            /// <summary>创建一个芯片根、形态或姿态范围。</summary>
            internal FeatureScope(
                string key,
                string labelKey,
                int order,
                FeatureScope parent,
                bool isRoot)
            {
                Key = key;
                LabelKey = labelKey;
                Order = order;
                Parent = parent;
                IsRoot = isRoot;
            }
        }
    }
}
