using System;
using System.Collections.Generic;
using BDP.Core.Information;
using Verse;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>没有专用展示策略时，按统一节点中的属性顺序显示全部属性。</summary>
    internal sealed class GenericChipFeatureRenderer : IChipFeatureRenderer
    {
        /// <summary>被专用展示器消费后仍需追加的属性 Id。</summary>
        private readonly ISet<string> consumedPropertyIds;

        /// <summary>创建指定已消费集合的通用展示器。</summary>
        internal GenericChipFeatureRenderer(IEnumerable<string> consumedPropertyIds = null)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            if (consumedPropertyIds != null)
            {
                foreach (string id in consumedPropertyIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        ids.Add(id);
                    }
                }
            }

            this.consumedPropertyIds = ids;
        }

        /// <summary>生成剩余属性的通用展示结果。</summary>
        public ChipFeatureRenderResult Render(
            ChipInformationNode feature,
            ChipFeatureRenderContext context)
        {
            List<ChipFeatureRenderLine> lines = new List<ChipFeatureRenderLine>();
            HashSet<string> emitted = new HashSet<string>(StringComparer.Ordinal);
            if (feature?.Properties != null)
            {
                for (int index = 0; index < feature.Properties.Count; index++)
                {
                    AppendLine(feature.Properties[index], lines, emitted);
                }
            }

            return new ChipFeatureRenderResult(feature, lines, null);
        }

        /// <summary>追加一项未消费且未重复的统一属性。</summary>
        private void AppendLine(
            ChipInformationProperty property,
            ICollection<ChipFeatureRenderLine> lines,
            ISet<string> emitted)
        {
            string id = property?.Key;
            if (property == null
                || string.IsNullOrWhiteSpace(id)
                || consumedPropertyIds.Contains(id)
                || !emitted.Add(id))
            {
                return;
            }

            lines.Add(new ChipFeatureRenderLine(property, GetPropertyLabel(property)));
        }

        /// <summary>读取属性名称；优先用节点标签，再查属性 Def，最后回退稳定键。</summary>
        internal static string GetPropertyLabel(ChipInformationProperty property)
        {
            if (!string.IsNullOrWhiteSpace(property?.LabelKey))
            {
                return property.LabelKey.Translate().ToString();
            }

            ChipFeaturePropertyDef definition = string.IsNullOrWhiteSpace(property?.Key)
                ? null
                : DefDatabase<ChipFeaturePropertyDef>.GetNamedSilentFail(property.Key);
            string label = definition?.LabelCap.ToString();
            return string.IsNullOrWhiteSpace(label) ? property?.Key : label;
        }
    }
}
