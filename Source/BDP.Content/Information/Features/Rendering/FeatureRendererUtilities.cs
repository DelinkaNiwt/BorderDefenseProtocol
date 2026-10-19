using System;
using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>专用展示器共享的类型化属性行构造工具。</summary>
    internal static class FeatureRendererUtilities
    {
        /// <summary>按功能结果顺序提取指定属性集合。</summary>
        internal static ChipFeatureRenderResult RenderKnown(
            ChipInformationNode feature,
            ISet<string> knownPropertyIds)
        {
            List<ChipFeatureRenderLine> lines = new List<ChipFeatureRenderLine>();
            HashSet<string> consumed = new HashSet<string>(StringComparer.Ordinal);
            if (feature?.Properties != null)
            {
                for (int index = 0; index < feature.Properties.Count; index++)
                {
                    ChipInformationProperty property = feature.Properties[index];
                    string id = property?.Key;
                    if (property != null
                        && !string.IsNullOrWhiteSpace(id)
                        && knownPropertyIds != null
                        && knownPropertyIds.Contains(id))
                    {
                        consumed.Add(id);
                        lines.Add(new ChipFeatureRenderLine(
                            property,
                            GenericChipFeatureRenderer.GetPropertyLabel(property)));
                    }
                }
            }

            return new ChipFeatureRenderResult(feature, lines, consumed);
        }
    }
}
