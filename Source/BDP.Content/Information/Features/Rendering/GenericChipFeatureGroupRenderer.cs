using System;
using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>从统一功能节点读取信息组展示行。</summary>
    internal sealed class GenericChipFeatureGroupRenderer
    {
        /// <summary>读取功能下的信息组节点。</summary>
        internal IReadOnlyList<ChipFeatureGroupRenderResult> RenderGroups(
            IReadOnlyList<ChipInformationNode> children)
        {
            return RenderKind(children, "feature_group", false);
        }

        /// <summary>读取功能或信息组下的一层子组节点。</summary>
        internal IReadOnlyList<ChipFeatureGroupRenderResult> RenderSubgroups(
            IReadOnlyList<ChipInformationNode> children)
        {
            return RenderKind(children, "feature_subgroup", true);
        }

        /// <summary>按统一节点类型保留每组的属性和子组。</summary>
        private IReadOnlyList<ChipFeatureGroupRenderResult> RenderKind(
            IReadOnlyList<ChipInformationNode> children,
            string kindKey,
            bool isSubgroup)
        {
            List<ChipFeatureGroupRenderResult> result = new List<ChipFeatureGroupRenderResult>();
            if (children != null)
            {
                for (int index = 0; index < children.Count; index++)
                {
                    ChipInformationNode node = children[index];
                    if (node == null
                        || !string.Equals(node.KindKey, kindKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    result.Add(new ChipFeatureGroupRenderResult(
                        node,
                        node.Key,
                        node.LabelKey,
                        node.Key,
                        isSubgroup,
                        RenderProperties(node.Properties),
                        RenderSubgroups(node.Children)));
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>把节点属性直接包装为绘制行。</summary>
        private static IReadOnlyList<ChipFeatureRenderLine> RenderProperties(
            IReadOnlyList<ChipInformationProperty> properties)
        {
            List<ChipFeatureRenderLine> result = new List<ChipFeatureRenderLine>();
            if (properties != null)
            {
                for (int index = 0; index < properties.Count; index++)
                {
                    ChipInformationProperty property = properties[index];
                    if (property != null)
                    {
                        result.Add(new ChipFeatureRenderLine(
                            property,
                            GenericChipFeatureRenderer.GetPropertyLabel(property)));
                    }
                }
            }

            return result.AsReadOnly();
        }
    }
}
