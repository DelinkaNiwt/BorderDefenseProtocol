using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>保留功能组或其一层子组语义的只读展示结果。</summary>
    internal sealed class ChipFeatureGroupRenderResult
    {
        /// <summary>玩家语义信息组 Def。</summary>
        internal ChipInformationNode Node { get; }

        /// <summary>信息组或子组实例稳定 Id。</summary>
        internal string InstanceId { get; }

        /// <summary>可选实例显示语言键。</summary>
        internal string LabelKey { get; }

        /// <summary>信息组或子组在功能树中的完整稳定路径。</summary>
        internal string Path { get; }

        /// <summary>该项是否为直接归属父功能或父组的一层子组。</summary>
        internal bool IsSubgroup { get; }

        /// <summary>当前信息组或子组的通用属性行。</summary>
        internal IReadOnlyList<ChipFeatureRenderLine> Lines { get; }

        /// <summary>当前信息组拥有的一层子组。</summary>
        internal IReadOnlyList<ChipFeatureGroupRenderResult> Subgroups { get; }

        /// <summary>创建只读组展示结果。</summary>
        internal ChipFeatureGroupRenderResult(
            ChipInformationNode node,
            string instanceId,
            string labelKey,
            string path,
            bool isSubgroup,
            IEnumerable<ChipFeatureRenderLine> lines,
            IEnumerable<ChipFeatureGroupRenderResult> subgroups = null)
        {
            Node = node;
            InstanceId = instanceId;
            LabelKey = labelKey;
            Path = path;
            IsSubgroup = isSubgroup;
            Lines = CopyLines(lines);
            Subgroups = CopySubgroups(subgroups);
        }

        /// <summary>复制通用属性行集合。</summary>
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

        /// <summary>复制子组集合。</summary>
        private static IReadOnlyList<ChipFeatureGroupRenderResult> CopySubgroups(
            IEnumerable<ChipFeatureGroupRenderResult> source)
        {
            List<ChipFeatureGroupRenderResult> result =
                new List<ChipFeatureGroupRenderResult>();
            if (source != null)
            {
                foreach (ChipFeatureGroupRenderResult subgroup in source)
                {
                    if (subgroup != null)
                    {
                        result.Add(subgroup);
                    }
                }
            }

            return result.AsReadOnly();
        }
    }
}
