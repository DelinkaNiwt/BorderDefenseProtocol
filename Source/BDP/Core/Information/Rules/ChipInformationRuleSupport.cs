using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 通用信息规则共用的类型化转换工具。
    /// </summary>
    internal static class ChipInformationRuleSupport
    {
        /// <summary>从事实集合创建字段结构声明。</summary>
        internal static IReadOnlyList<ChipInformationFieldSchema> DescribeFields(
            IEnumerable<ChipInformationFactValue> facts)
        {
            List<ChipInformationFieldSchema> result =
                new List<ChipInformationFieldSchema>();
            if (facts == null)
            {
                return result;
            }

            foreach (ChipInformationFactValue fact in facts)
            {
                if (fact == null || string.IsNullOrWhiteSpace(fact.FieldKey))
                {
                    continue;
                }

                result.Add(new ChipInformationFieldSchema(
                    fact.FieldKey,
                    fact.LabelKey,
                    fact.ValueKind,
                    fact.UnitKey,
                    fact.Value == null || !HasAppliedSource(fact.Provenance)));
            }

            return result;
        }

        /// <summary>把事实字段转换为规则贡献。</summary>
        internal static IReadOnlyList<ChipInformationContribution> ResolveFields(
            string blockKey,
            string kind,
            string labelKey,
            int order,
            IEnumerable<ChipInformationFactValue> facts)
        {
            List<ChipInformationContribution> result =
                new List<ChipInformationContribution>();
            if (facts == null)
            {
                return result;
            }

            foreach (ChipInformationFactValue fact in facts)
            {
                if (fact == null || string.IsNullOrWhiteSpace(fact.FieldKey))
                {
                    continue;
                }

                ChipInformationFieldSchema schema = new ChipInformationFieldSchema(
                    fact.FieldKey,
                    fact.LabelKey,
                    fact.ValueKind,
                    fact.UnitKey,
                    fact.Value == null || !HasAppliedSource(fact.Provenance));
                ChipInformationProperty property = fact.Value == null
                    || !HasAppliedSource(fact.Provenance)
                    ? null
                    : new ChipInformationProperty(
                        fact.FieldKey,
                        fact.LabelKey,
                        fact.ValueKind,
                        fact.Value,
                        fact.UnitKey,
                        fact.Provenance);
                result.Add(new ChipInformationContribution(
                    blockKey,
                    kind,
                    labelKey,
                    order,
                    schema,
                    property));
            }

            return result;
        }

        /// <summary>判断事实是否由至少一个实际参与的来源提供。</summary>
        private static bool HasAppliedSource(
            IReadOnlyList<ChipInformationProvenance> provenance)
        {
            if (provenance == null || provenance.Count == 0)
            {
                return true;
            }

            for (int index = 0; index < provenance.Count; index++)
            {
                if (provenance[index]?.Applied == true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>判断集合是否有可解析字段。</summary>
        internal static bool HasFields(IEnumerable<ChipInformationFactValue> facts)
        {
            if (facts == null)
            {
                return false;
            }

            foreach (ChipInformationFactValue fact in facts)
            {
                if (fact != null && !string.IsNullOrWhiteSpace(fact.FieldKey))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>按通道键匹配表达能力。</summary>
        internal static bool MatchesChannel(
            string channelKey,
            params string[] acceptedKeys)
        {
            if (string.IsNullOrWhiteSpace(channelKey) || acceptedKeys == null)
            {
                return false;
            }

            for (int index = 0; index < acceptedKeys.Length; index++)
            {
                if (string.Equals(
                    channelKey,
                    acceptedKeys[index],
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>创建表达来源块键，保持不同来源互不覆盖。</summary>
        internal static string GetExpressionBlockKey(
            string channelKey,
            string expressionKey,
            string fallbackKey)
        {
            string channel = string.IsNullOrWhiteSpace(channelKey)
                ? fallbackKey
                : channelKey;
            string expression = string.IsNullOrWhiteSpace(expressionKey)
                ? "default"
                : expressionKey;
            return channel + ":" + expression;
        }

        /// <summary>把当前表达事实转换为信息贡献。</summary>
        internal static void AddExpressionContributions(
            string blockKind,
            string blockLabelKey,
            int order,
            ChipInformationExpressionFact expression,
            ICollection<ChipInformationContribution> target,
            string fallbackChannelKey)
        {
            if (expression == null || target == null)
            {
                return;
            }

            string blockKey = GetExpressionBlockKey(
                expression.ChannelKey,
                expression.ExpressionKey,
                fallbackChannelKey);
            List<ChipInformationFactValue> fields =
                new List<ChipInformationFactValue>();
            if (expression.Fields != null)
            {
                for (int index = 0; index < expression.Fields.Count; index++)
                {
                    ChipInformationFactValue fact = expression.Fields[index];
                    if (fact != null)
                    {
                        fields.Add(fact);
                    }
                }
            }

            if (fields.Count > 0)
            {
                IReadOnlyList<ChipInformationContribution> contributions =
                    ResolveFields(
                        blockKey,
                        blockKind,
                        blockLabelKey,
                        order,
                        fields);
                for (int index = 0; index < contributions.Count; index++)
                {
                    target.Add(contributions[index]);
                }
            }

            AddFactBlockContributions(
                blockKey,
                expression.Blocks,
                target,
                new HashSet<ChipInformationFactBlock>());

        }

        /// <summary>把当前表达事实转换为块结构声明。</summary>
        internal static void AddExpressionSchemas(
            string blockKind,
            string blockLabelKey,
            int order,
            ChipInformationExpressionFact expression,
            ICollection<ChipInformationBlockSchema> target,
            string fallbackChannelKey)
        {
            if (expression == null || target == null)
            {
                return;
            }

            string blockKey = GetExpressionBlockKey(
                expression.ChannelKey,
                expression.ExpressionKey,
                fallbackChannelKey);
            target.Add(new ChipInformationBlockSchema(
                blockKey,
                blockKind,
                string.IsNullOrWhiteSpace(expression.LabelKey)
                    ? blockLabelKey
                    : expression.LabelKey,
                order,
                DescribeFields(expression.Fields),
                DescribeChildBlocks(
                    expression.Blocks,
                    new HashSet<ChipInformationFactBlock>())));

        }

        /// <summary>把根级结构化事实块转换为字段贡献。</summary>
        internal static IReadOnlyList<ChipInformationContribution> ResolveFactBlocks(
            IEnumerable<ChipInformationFactBlock> blocks)
        {
            List<ChipInformationContribution> result = new List<ChipInformationContribution>();
            AddFactBlockContributions(
                null,
                blocks,
                result,
                new HashSet<ChipInformationFactBlock>());
            return result;
        }

        /// <summary>把根级结构化事实块转换为结构声明。</summary>
        internal static IReadOnlyList<ChipInformationBlockSchema> DescribeFactBlocks(
            IEnumerable<ChipInformationFactBlock> blocks)
        {
            List<ChipInformationBlockSchema> result = new List<ChipInformationBlockSchema>();
            AddFactBlockSchemas(
                blocks,
                result,
                new HashSet<ChipInformationFactBlock>());
            return result;
        }

        /// <summary>递归转换结构化事实块的字段贡献。</summary>
        private static void AddFactBlockContributions(
            string parentBlockKey,
            IEnumerable<ChipInformationFactBlock> blocks,
            ICollection<ChipInformationContribution> target,
            ISet<ChipInformationFactBlock> path)
        {
            if (blocks == null || target == null || path == null)
            {
                return;
            }

            foreach (ChipInformationFactBlock block in blocks)
            {
                if (block == null || string.IsNullOrWhiteSpace(block.BlockKey)
                    || !path.Add(block))
                {
                    continue;
                }

                IReadOnlyList<ChipInformationContribution> fields = ResolveFields(
                    block.BlockKey,
                    block.Kind,
                    block.LabelKey,
                    block.Order,
                    block.Fields);
                for (int index = 0; index < fields.Count; index++)
                {
                    ChipInformationContribution field = fields[index];
                    target.Add(new ChipInformationContribution(
                        field.BlockKey,
                        field.Kind,
                        field.LabelKey,
                        field.Order,
                        field.FieldSchema,
                        field.Property,
                        parentBlockKey));
                }

                string currentBlockIdentity = string.IsNullOrWhiteSpace(parentBlockKey)
                    ? block.BlockKey
                    : parentBlockKey + "\u001f" + block.BlockKey;

                AddFactBlockContributions(
                    currentBlockIdentity,
                    block.ChildBlocks,
                    target,
                    path);
                path.Remove(block);
            }
        }

        /// <summary>递归转换结构化事实块的结构声明。</summary>
        private static void AddFactBlockSchemas(
            IEnumerable<ChipInformationFactBlock> blocks,
            ICollection<ChipInformationBlockSchema> target,
            ISet<ChipInformationFactBlock> path)
        {
            if (blocks == null || target == null || path == null)
            {
                return;
            }

            foreach (ChipInformationFactBlock block in blocks)
            {
                if (block == null || string.IsNullOrWhiteSpace(block.BlockKey)
                    || !path.Add(block))
                {
                    continue;
                }

                target.Add(new ChipInformationBlockSchema(
                    block.BlockKey,
                    block.Kind,
                    block.LabelKey,
                    block.Order,
                    DescribeFields(block.Fields),
                    DescribeChildBlocks(block.ChildBlocks, path)));
                path.Remove(block);
            }
        }

        /// <summary>递归创建结构化子块声明。</summary>
        private static IReadOnlyList<ChipInformationBlockSchema> DescribeChildBlocks(
            IEnumerable<ChipInformationFactBlock> blocks,
            ISet<ChipInformationFactBlock> path)
        {
            List<ChipInformationBlockSchema> result = new List<ChipInformationBlockSchema>();
            AddFactBlockSchemas(blocks, result, path);
            return result;

        }
    }
}
