using System;
using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>一行玩家向功能属性展示结果。</summary>
    internal sealed class ChipFeatureRenderLine
    {
        /// <summary>对应的解析属性；状态异常时仍保留该对象。</summary>
        internal ChipInformationProperty Property { get; }

        /// <summary>功能属性的玩家向名称。</summary>
        internal string Label { get; }

        /// <summary>当前显示值是否与同一属性的图表比较基准不同。</summary>
        internal bool HasChangedBaselineValue { get; }

        /// <summary>创建展示行。</summary>
        internal ChipFeatureRenderLine(
            ChipInformationProperty property,
            string label,
            bool hasChangedBaselineValue = false)
        {
            Property = property;
            Label = label;
            HasChangedBaselineValue = hasChangedBaselineValue;
        }
    }

    /// <summary>功能展示器输出的结构化结果。</summary>
    internal sealed class ChipFeatureRenderResult
    {
        /// <summary>对应的功能解析结果。</summary>
        internal ChipInformationNode Feature { get; }

        /// <summary>当前展示器生成的属性行。</summary>
        internal IReadOnlyList<ChipFeatureRenderLine> Lines { get; }

        /// <summary>功能声明中的语义信息组。</summary>
        internal IReadOnlyList<ChipFeatureGroupRenderResult> Groups { get; }

        /// <summary>直接归属于整个功能的额外效果等子组。</summary>
        internal IReadOnlyList<ChipFeatureGroupRenderResult> Subgroups { get; }

        /// <summary>已由专用展示器消费的属性稳定 Id。</summary>
        internal IReadOnlyCollection<string> ConsumedPropertyIds { get; }

        /// <summary>子功能展示结果。</summary>
        internal IReadOnlyList<ChipFeatureRenderResult> Children { get; }

        /// <summary>创建只读展示结果。</summary>
        internal ChipFeatureRenderResult(
            ChipInformationNode feature,
            IEnumerable<ChipFeatureRenderLine> lines,
            IEnumerable<string> consumedPropertyIds,
            IEnumerable<ChipFeatureRenderResult> children = null,
            IEnumerable<ChipFeatureGroupRenderResult> groups = null,
            IEnumerable<ChipFeatureGroupRenderResult> subgroups = null)
        {
            Feature = feature;
            Lines = CopyLines(lines);
            Groups = CopyGroups(groups);
            Subgroups = CopyGroups(subgroups);
            ConsumedPropertyIds = CopyIds(consumedPropertyIds);
            Children = CopyChildren(children);
        }

        /// <summary>复制信息组展示结果。</summary>
        private static IReadOnlyList<ChipFeatureGroupRenderResult> CopyGroups(
            IEnumerable<ChipFeatureGroupRenderResult> source)
        {
            List<ChipFeatureGroupRenderResult> result =
                new List<ChipFeatureGroupRenderResult>();
            if (source != null)
            {
                foreach (ChipFeatureGroupRenderResult group in source)
                {
                    if (group != null)
                    {
                        result.Add(group);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>按顺序复制展示行。</summary>
        private static IReadOnlyList<ChipFeatureRenderLine> CopyLines(
            IEnumerable<ChipFeatureRenderLine> source)
        {
            List<ChipFeatureRenderLine> result = new List<ChipFeatureRenderLine>();
            if (source != null)
            {
                foreach (ChipFeatureRenderLine line in source)
                {
                    if (line != null)
                    {
                        result.Add(line);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>复制已消费属性 Id。</summary>
        private static IReadOnlyCollection<string> CopyIds(
            IEnumerable<string> source)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            if (source != null)
            {
                foreach (string value in source)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        result.Add(value);
                    }
                }
            }

            return result;
        }

        /// <summary>复制子功能结果。</summary>
        private static IReadOnlyList<ChipFeatureRenderResult> CopyChildren(
            IEnumerable<ChipFeatureRenderResult> source)
        {
            List<ChipFeatureRenderResult> result = new List<ChipFeatureRenderResult>();
            if (source != null)
            {
                foreach (ChipFeatureRenderResult child in source)
                {
                    if (child != null)
                    {
                        result.Add(child);
                    }
                }
            }

            return result.AsReadOnly();
        }
    }
}
