using System;
using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>按节点类型读取统一信息树中的展示入口，不转换或复制模型。</summary>
    internal static class ChipInformationNodeQuery
    {
        /// <summary>读取指定节点下直接声明的功能节点。</summary>
        internal static IReadOnlyList<ChipInformationNode> ReadFeatures(
            ChipInformationNode parent)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (parent?.Children == null)
            {
                return result.AsReadOnly();
            }

            for (int index = 0; index < parent.Children.Count; index++)
            {
                ChipInformationNode child = parent.Children[index];
                if (IsKind(child, "feature"))
                {
                    result.Add(child);
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>按树中顺序递归读取所有上下文节点。</summary>
        internal static IReadOnlyList<ChipInformationNode> ReadContexts(
            ChipInformationNode root)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            AppendContexts(root?.Children, result);
            return result.AsReadOnly();
        }

        /// <summary>读取根节点下的一般信息节点。</summary>
        internal static IReadOnlyList<ChipInformationNode> ReadInformationNodes(
            ChipInformationNode root)
        {
            List<ChipInformationNode> result = new List<ChipInformationNode>();
            if (root?.Children == null)
            {
                return result.AsReadOnly();
            }

            for (int index = 0; index < root.Children.Count; index++)
            {
                ChipInformationNode child = root.Children[index];
                if (child != null
                    && !IsKind(child, "feature")
                    && !IsKind(child, "context")
                    && !IsKind(child, "relation"))
                {
                    result.Add(child);
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>递归收集上下文，不解析或重建其子节点。</summary>
        private static void AppendContexts(
            IReadOnlyList<ChipInformationNode> source,
            ICollection<ChipInformationNode> target)
        {
            if (source == null || target == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ChipInformationNode node = source[index];
                if (!IsKind(node, "context"))
                {
                    continue;
                }

                target.Add(node);
                AppendContexts(node.Children, target);
            }
        }

        /// <summary>按稳定类型键判断节点类别。</summary>
        private static bool IsKind(ChipInformationNode node, string kindKey)
        {
            return node != null
                && string.Equals(node.KindKey, kindKey, StringComparison.Ordinal);
        }
    }
}
