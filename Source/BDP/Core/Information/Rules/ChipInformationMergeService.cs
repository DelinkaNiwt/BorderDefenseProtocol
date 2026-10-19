using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息规则贡献合并器。
    /// </summary>
    internal sealed class ChipInformationMergeService
    {
        /// <summary>把贡献合并为信息模型所需的结构和值。</summary>
        internal ChipInformationMergeResult Merge(
            IEnumerable<ChipInformationBlockSchema> schemas,
            IEnumerable<ChipInformationContribution> contributions)
        {
            Dictionary<string, BlockAccumulator> blocks =
                new Dictionary<string, BlockAccumulator>(StringComparer.Ordinal);
            List<ChipInformationDiagnostic> diagnostics =
                new List<ChipInformationDiagnostic>();

            MergeSchemas(blocks, schemas, null, diagnostics);
            MergeContributions(blocks, contributions, diagnostics);

            List<ChipInformationNode> nodes = new List<ChipInformationNode>();
            List<BlockAccumulator> roots = GetChildren(blocks, null);
            for (int index = 0; index < roots.Count; index++)
            {
                BlockAccumulator root = roots[index];
                if (HasOutput(root, blocks, new HashSet<string>(StringComparer.Ordinal)))
                {
                    nodes.Add(BuildNode(root, blocks, new HashSet<string>(StringComparer.Ordinal)));
                }
            }

            return new ChipInformationMergeResult(nodes, diagnostics);
        }

        /// <summary>递归合并规则声明。</summary>
        private static void MergeSchemas(
            IDictionary<string, BlockAccumulator> blocks,
            IEnumerable<ChipInformationBlockSchema> schemas,
            string parentIdentityKey,
            ICollection<ChipInformationDiagnostic> diagnostics)
        {
            if (schemas == null)
            {
                return;
            }

            foreach (ChipInformationBlockSchema schema in schemas)
            {
                if (schema == null || string.IsNullOrWhiteSpace(schema.BlockKey))
                {
                    continue;
                }

                BlockAccumulator block = GetBlock(blocks, parentIdentityKey, schema.BlockKey);
                block.ApplySchema(schema, diagnostics);
                MergeSchemas(blocks, schema.ChildBlocks, block.IdentityKey, diagnostics);
            }
        }

        /// <summary>合并规则字段贡献。</summary>
        private static void MergeContributions(
            IDictionary<string, BlockAccumulator> blocks,
            IEnumerable<ChipInformationContribution> contributions,
            ICollection<ChipInformationDiagnostic> diagnostics)
        {
            if (contributions == null)
            {
                return;
            }

            foreach (ChipInformationContribution contribution in contributions)
            {
                if (contribution == null || string.IsNullOrWhiteSpace(contribution.BlockKey))
                {
                    continue;
                }

                BlockAccumulator block = GetBlock(
                    blocks,
                    contribution.ParentBlockKey,
                    contribution.BlockKey);
                block.ApplyContribution(contribution, diagnostics);
            }
        }

        /// <summary>按父块和块键取得或创建合并器。</summary>
        private static BlockAccumulator GetBlock(
            IDictionary<string, BlockAccumulator> blocks,
            string parentIdentityKey,
            string blockKey)
        {
            string identityKey = MakeIdentityKey(parentIdentityKey, blockKey);
            BlockAccumulator block;
            if (!blocks.TryGetValue(identityKey, out block))
            {
                block = new BlockAccumulator(identityKey, parentIdentityKey, blockKey);
                blocks.Add(identityKey, block);
            }

            return block;
        }

        /// <summary>生成内部块路径键。</summary>
        private static string MakeIdentityKey(string parentIdentityKey, string blockKey)
        {
            return string.IsNullOrWhiteSpace(parentIdentityKey)
                ? blockKey
                : parentIdentityKey + "\u001f" + blockKey;
        }

        /// <summary>取得指定父块下的所有直接子块。</summary>
        private static List<BlockAccumulator> GetChildren(
            IDictionary<string, BlockAccumulator> blocks,
            string parentIdentityKey)
        {
            List<BlockAccumulator> result = new List<BlockAccumulator>();
            foreach (BlockAccumulator block in blocks.Values)
            {
                if (string.Equals(block.ParentIdentityKey, parentIdentityKey, StringComparison.Ordinal))
                {
                    result.Add(block);
                }
            }

            result.Sort(CompareAccumulatorOrder);
            return result;
        }

        /// <summary>递归判断信息块是否有实际输出。</summary>
        private static bool HasOutput(
            BlockAccumulator block,
            IDictionary<string, BlockAccumulator> blocks,
            ISet<string> path)
        {
            if (block == null || path == null || !path.Add(block.IdentityKey))
            {
                return false;
            }

            if (block.Properties.Count > 0)
            {
                path.Remove(block.IdentityKey);
                return true;
            }

            List<BlockAccumulator> children = GetChildren(blocks, block.IdentityKey);
            for (int index = 0; index < children.Count; index++)
            {
                if (HasOutput(children[index], blocks, path))
                {
                    path.Remove(block.IdentityKey);
                    return true;
                }
            }

            path.Remove(block.IdentityKey);
            return false;
        }

        /// <summary>递归构建当前统一信息节点。</summary>
        private static ChipInformationNode BuildNode(
            BlockAccumulator block,
            IDictionary<string, BlockAccumulator> blocks,
            ISet<string> path)
        {
            if (block == null || path == null || !path.Add(block.IdentityKey))
            {
                return null;
            }

            List<ChipInformationNode> children = new List<ChipInformationNode>();
            List<BlockAccumulator> childAccumulators = GetChildren(blocks, block.IdentityKey);
            for (int index = 0; index < childAccumulators.Count; index++)
            {
                BlockAccumulator childAccumulator = childAccumulators[index];
                if (!HasOutput(childAccumulator, blocks, new HashSet<string>(path, StringComparer.Ordinal)))
                {
                    continue;
                }

                ChipInformationNode child = BuildNode(childAccumulator, blocks, path);
                if (child != null)
                {
                    children.Add(child);
                }
            }

            path.Remove(block.IdentityKey);
            return block.BuildNode(children);
        }

        /// <summary>按内部信息块顺序排序。</summary>
        private static int CompareAccumulatorOrder(
            BlockAccumulator left,
            BlockAccumulator right)
        {
            int order = left.Order.CompareTo(right.Order);
            return order != 0
                ? order
                : string.CompareOrdinal(left.BlockKey, right.BlockKey);
        }

        /// <summary>内部信息块合并状态。</summary>
        private sealed class BlockAccumulator
        {
            /// <summary>内部块路径键。</summary>
            internal string IdentityKey { get; private set; }

            /// <summary>父内部块路径键。</summary>
            internal string ParentIdentityKey { get; private set; }

            /// <summary>信息块键。</summary>
            internal string BlockKey { get; private set; }

            /// <summary>信息块语义类型。</summary>
            private string kind;

            /// <summary>信息块标题键。</summary>
            private string labelKey;

            /// <summary>信息块顺序。</summary>
            internal int Order { get; private set; }

            /// <summary>字段声明按键索引。</summary>
            private readonly Dictionary<string, ChipInformationFieldSchema> schemas =
                new Dictionary<string, ChipInformationFieldSchema>(StringComparer.Ordinal);

            /// <summary>字段值按键索引。</summary>
            internal readonly Dictionary<string, ChipInformationProperty> Properties =
                new Dictionary<string, ChipInformationProperty>(StringComparer.Ordinal);

            /// <summary>发生冲突后不再输出的字段键。</summary>
            private readonly HashSet<string> rejectedProperties =
                new HashSet<string>(StringComparer.Ordinal);

            /// <summary>创建内部信息块状态。</summary>
            internal BlockAccumulator(string identityKey, string parentIdentityKey, string blockKey)
            {
                IdentityKey = identityKey;
                ParentIdentityKey = parentIdentityKey;
                BlockKey = blockKey;
            }

            /// <summary>合并信息块声明。</summary>
            internal void ApplySchema(
                ChipInformationBlockSchema schema,
                ICollection<ChipInformationDiagnostic> diagnostics)
            {
                kind = PreferText(kind, schema.Kind);
                labelKey = PreferText(labelKey, schema.LabelKey);
                Order = Order == 0 ? schema.Order : Math.Min(Order, schema.Order);
                if (schema.Fields == null)
                {
                    return;
                }

                for (int index = 0; index < schema.Fields.Count; index++)
                {
                    ChipInformationFieldSchema field = schema.Fields[index];
                    if (field == null || string.IsNullOrWhiteSpace(field.FieldKey))
                    {
                        continue;
                    }

                    ChipInformationFieldSchema existing;
                    if (schemas.TryGetValue(field.FieldKey, out existing)
                        && existing.ValueKind != field.ValueKind)
                    {
                        diagnostics.Add(new ChipInformationDiagnostic(
                            "information.schema_field_type_conflict",
                            IdentityKey,
                            field.FieldKey,
                            ChipInformationDiagnosticSeverity.Error,
                            "同一字段键出现不同值类型。"));
                        continue;
                    }

                    schemas[field.FieldKey] = field;
                }
            }

            /// <summary>合并字段贡献。</summary>
            internal void ApplyContribution(
                ChipInformationContribution contribution,
                ICollection<ChipInformationDiagnostic> diagnostics)
            {
                kind = PreferText(kind, contribution.Kind);
                labelKey = PreferText(labelKey, contribution.LabelKey);
                Order = Order == 0 ? contribution.Order : Math.Min(Order, contribution.Order);
                if (contribution.FieldSchema != null)
                {
                    ApplySchema(
                        new ChipInformationBlockSchema(
                            BlockKey,
                            contribution.Kind,
                            contribution.LabelKey,
                            contribution.Order,
                            new[] { contribution.FieldSchema }),
                        diagnostics);
                }

                if (contribution.Property == null
                    || string.IsNullOrWhiteSpace(contribution.Property.Key))
                {
                    return;
                }

                if (rejectedProperties.Contains(contribution.Property.Key))
                {
                    return;
                }

                ChipInformationProperty existing;
                if (!Properties.TryGetValue(contribution.Property.Key, out existing))
                {
                    Properties.Add(contribution.Property.Key, contribution.Property);
                    return;
                }

                ChipInformationProperty merged = MergeProperty(
                    existing,
                    contribution.Property,
                    IdentityKey,
                    diagnostics);
                if (merged == null)
                {
                    Properties.Remove(contribution.Property.Key);
                    rejectedProperties.Add(contribution.Property.Key);
                    return;
                }

                Properties[contribution.Property.Key] = merged;
            }

            /// <summary>构建合并后的统一信息节点。</summary>
            internal ChipInformationNode BuildNode(
                IEnumerable<ChipInformationNode> children)
            {
                List<ChipInformationProperty> properties =
                    new List<ChipInformationProperty>(Properties.Values);
                properties.Sort(CompareProperty);
                return new ChipInformationNode(
                    BlockKey,
                    kind,
                    null,
                    labelKey,
                    Order,
                    properties,
                    children);
            }

            /// <summary>合并两个同键属性。</summary>
            private static ChipInformationProperty MergeProperty(
                ChipInformationProperty left,
                ChipInformationProperty right,
                string blockKey,
                ICollection<ChipInformationDiagnostic> diagnostics)
            {
                List<ChipInformationProvenance> provenance =
                    new List<ChipInformationProvenance>();
                AddProvenance(provenance, left.Sources);
                AddProvenance(provenance, right.Sources);

                if (left.ValueKind != right.ValueKind)
                {
                    diagnostics.Add(new ChipInformationDiagnostic(
                        "information.field_type_conflict",
                        blockKey,
                        left.Key,
                        ChipInformationDiagnosticSeverity.Error,
                        "同一字段键出现不同值类型。"));
                    return null;
                }

                int leftPriority = SourcePriority(left);
                int rightPriority = SourcePriority(right);
                if (leftPriority == rightPriority
                    && !AreEqual(left.Value, right.Value)
                    && HasAppliedValue(left)
                    && HasAppliedValue(right))
                {
                    diagnostics.Add(new ChipInformationDiagnostic(
                        "information.field_value_conflict",
                        blockKey,
                        left.Key,
                        ChipInformationDiagnosticSeverity.Error,
                        "同一字段键的来源无法按优先级裁定。"));
                    return null;
                }

                ChipInformationProperty winner = rightPriority > leftPriority ? right : left;
                return new ChipInformationProperty(
                    winner.Key,
                    PreferText(winner.LabelKey, left.LabelKey),
                    winner.ValueKind,
                    winner.Value,
                    PreferText(winner.UnitKey, left.UnitKey),
                    provenance);
            }

            /// <summary>判断值是否来自实际参与计算的来源。</summary>
            private static bool HasAppliedValue(ChipInformationProperty property)
            {
                if (property?.Sources == null || property.Sources.Count == 0)
                {
                    return true;
                }

                for (int index = 0; index < property.Sources.Count; index++)
                {
                    if (property.Sources[index]?.Applied == true)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>取得字段最高来源优先级。</summary>
            private static int SourcePriority(ChipInformationProperty field)
            {
                int priority = 0;
                if (field.Sources == null)
                {
                    return priority;
                }

                for (int index = 0; index < field.Sources.Count; index++)
                {
                    ChipInformationProvenance source = field.Sources[index];
                    if (source == null || !source.Applied)
                    {
                        continue;
                    }

                    priority = Math.Max(priority, SourcePriority(source.SourceKind));
                }

                return priority;
            }

            /// <summary>把来源类别转换成固定优先级。</summary>
            private static int SourcePriority(ChipInformationSourceKind sourceKind)
            {
                switch (sourceKind)
                {
                    case ChipInformationSourceKind.RuntimeResult:
                        return 400;
                    case ChipInformationSourceKind.EffectiveDefinition:
                        return 300;
                    case ChipInformationSourceKind.Variant:
                        return 250;
                    case ChipInformationSourceKind.DeclaredDefinition:
                        return 200;
                    case ChipInformationSourceKind.BaseDefinition:
                        return 100;
                    case ChipInformationSourceKind.DerivedFact:
                        return 50;
                    default:
                        return 150;
                }
            }

            /// <summary>比较两个类型化值是否相同。</summary>
            private static bool AreEqual(
                ChipInformationValue left,
                ChipInformationValue right)
            {
                if (left == null || right == null)
                {
                    return left == right;
                }

                if (left.Kind != right.Kind)
                {
                    return false;
                }

                switch (left.Kind)
                {
                    case ChipInformationValueKind.Text:
                        return string.Equals(left.Text, right.Text, StringComparison.Ordinal);
                    case ChipInformationValueKind.Boolean:
                        return left.Boolean == right.Boolean;
                    case ChipInformationValueKind.Integer:
                        return left.Integer == right.Integer;
                    case ChipInformationValueKind.Number:
                    case ChipInformationValueKind.Percentage:
                    case ChipInformationValueKind.Duration:
                        return Math.Abs(left.Number - right.Number) < 0.0001f;
                    case ChipInformationValueKind.Enum:
                        return left.DisplaySourceKind == right.DisplaySourceKind
                            && string.Equals(left.EnumKey, right.EnumKey, StringComparison.Ordinal)
                            && string.Equals(left.DisplayKey, right.DisplayKey, StringComparison.Ordinal)
                            && string.Equals(left.DefinitionTypeKey, right.DefinitionTypeKey, StringComparison.Ordinal)
                            && string.Equals(left.DefinitionName, right.DefinitionName, StringComparison.Ordinal);
                    case ChipInformationValueKind.List:
                        if (left.Items == null || right.Items == null
                            || left.Items.Count != right.Items.Count)
                        {
                            return false;
                        }

                        for (int index = 0; index < left.Items.Count; index++)
                        {
                            if (!AreEqual(left.Items[index], right.Items[index]))
                            {
                                return false;
                            }
                        }

                        return true;
                    case ChipInformationValueKind.Record:
                        if (left.Fields == null || right.Fields == null
                            || left.Fields.Count != right.Fields.Count)
                        {
                            return false;
                        }

                        for (int index = 0; index < left.Fields.Count; index++)
                        {
                            ChipInformationValueField leftField = left.Fields[index];
                            ChipInformationValueField rightField = right.Fields[index];
                            if (leftField == null || rightField == null
                                || !string.Equals(leftField.Key, rightField.Key, StringComparison.Ordinal)
                                || !string.Equals(leftField.UnitKey, rightField.UnitKey, StringComparison.Ordinal)
                                || !AreEqual(leftField.Value, rightField.Value))
                            {
                                return false;
                            }
                        }

                        return true;
                    default:
                        return false;
                }
            }

            /// <summary>复制来源记录。</summary>
            private static void AddProvenance(
                ICollection<ChipInformationProvenance> target,
                IReadOnlyList<ChipInformationProvenance> source)
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

            /// <summary>优先选用非空文本。</summary>
            private static string PreferText(string left, string right)
            {
                return !string.IsNullOrWhiteSpace(left) ? left : right;
            }

            /// <summary>按属性键排序。</summary>
            private static int CompareProperty(
                ChipInformationProperty left,
                ChipInformationProperty right)
            {
                return string.CompareOrdinal(left.Key, right.Key);
            }
        }
    }

    /// <summary>信息贡献合并器的输出。</summary>
    internal sealed class ChipInformationMergeResult
    {
        /// <summary>合并后的一般信息节点。</summary>
        internal IReadOnlyList<ChipInformationNode> Nodes { get; }

        /// <summary>合并诊断。</summary>
        internal IReadOnlyList<ChipInformationDiagnostic> Diagnostics { get; }

        /// <summary>创建合并结果。</summary>
        internal ChipInformationMergeResult(
            IReadOnlyList<ChipInformationNode> nodes,
            IReadOnlyList<ChipInformationDiagnostic> diagnostics)
        {
            Nodes = nodes;
            Diagnostics = diagnostics;
        }
    }
}
