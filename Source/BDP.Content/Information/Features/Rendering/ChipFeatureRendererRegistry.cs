using System;
using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>
    /// 按 FeatureDef 精确选择专用展示器，并始终追加通用属性回退。
    /// 注册表只影响展示策略，不参与功能识别和解析。
    /// </summary>
    internal sealed class ChipFeatureRendererRegistry
    {
        /// <summary>默认 Content 展示器注册表。</summary>
        internal static ChipFeatureRendererRegistry CreateDefault()
        {
            ChipFeatureRendererRegistry registry = new ChipFeatureRendererRegistry();
            registry.Register("BDP_Feature_RangedAttack", new RangedAttackFeatureRenderer());
            registry.Register("BDP_Feature_MeleeAttack", new MeleeAttackFeatureRenderer());
            registry.Register("BDP_Feature_Shield", new ShieldFeatureRenderer());
            return registry;
        }

        /// <summary>FeatureDef 到专用展示器的精确映射。</summary>
        private readonly Dictionary<string, IChipFeatureRenderer> renderers =
            new Dictionary<string, IChipFeatureRenderer>(StringComparer.Ordinal);

        /// <summary>所有功能共用的通用信息组投影器。</summary>
        private readonly GenericChipFeatureGroupRenderer groupRenderer =
            new GenericChipFeatureGroupRenderer();

        /// <summary>注册一个精确 FeatureDef 展示器。</summary>
        internal void Register(string featureDefName, IChipFeatureRenderer renderer)
        {
            if (string.IsNullOrWhiteSpace(featureDefName) || renderer == null)
            {
                throw new ArgumentException("功能展示器注册需要稳定 FeatureDef 和非空展示器。");
            }

            renderers[featureDefName] = renderer;
        }

        /// <summary>解析一个功能并追加未知属性通用回退。</summary>
        internal ChipFeatureRenderResult Render(
            ChipInformationNode feature,
            ChipFeatureRenderContext context)
        {
            if (feature == null)
            {
                return new ChipFeatureRenderResult(null, null, null);
            }

            IChipFeatureRenderer specialized;
            ChipFeatureRenderResult specializedResult = null;
            if (!string.IsNullOrWhiteSpace(feature.DefinitionKey)
                && renderers.TryGetValue(feature.DefinitionKey, out specialized))
            {
                specializedResult = specialized.Render(feature, context);
            }

            IReadOnlyCollection<string> consumed = specializedResult?.ConsumedPropertyIds;
            ChipFeatureRenderResult genericResult = new GenericChipFeatureRenderer(consumed)
                .Render(feature, context);
            List<ChipFeatureRenderLine> lines = new List<ChipFeatureRenderLine>();
            if (specializedResult?.Lines != null)
            {
                lines.AddRange(specializedResult.Lines);
            }

            if (genericResult.Lines != null)
            {
                lines.AddRange(genericResult.Lines);
            }

            List<ChipFeatureRenderResult> children = new List<ChipFeatureRenderResult>();
            List<ChipInformationNode> groupNodes = new List<ChipInformationNode>();
            List<ChipInformationNode> subgroupNodes = new List<ChipInformationNode>();
            if (feature.Children != null)
            {
                for (int index = 0; index < feature.Children.Count; index++)
                {
                    ChipInformationNode child = feature.Children[index];
                    if (string.Equals(child?.KindKey, "feature", StringComparison.Ordinal))
                    {
                        children.Add(Render(child, context));
                    }
                    else if (string.Equals(child?.KindKey, "feature_group", StringComparison.Ordinal))
                    {
                        groupNodes.Add(child);
                    }
                    else if (string.Equals(child?.KindKey, "feature_subgroup", StringComparison.Ordinal))
                    {
                        subgroupNodes.Add(child);
                    }
                }
            }

            IReadOnlyList<ChipFeatureGroupRenderResult> groups =
                groupRenderer.RenderGroups(groupNodes);
            IReadOnlyList<ChipFeatureGroupRenderResult> subgroups =
                groupRenderer.RenderSubgroups(subgroupNodes);

            return new ChipFeatureRenderResult(
                feature,
                lines,
                specializedResult?.ConsumedPropertyIds,
                children,
                groups,
                subgroups);
        }

        /// <summary>按顶层声明顺序解析一组功能。</summary>
        internal IReadOnlyList<ChipFeatureRenderResult> RenderAll(
            IReadOnlyList<ChipInformationNode> features,
            string contextKey = null)
        {
            List<ChipFeatureRenderResult> results = new List<ChipFeatureRenderResult>();
            if (features != null)
            {
                for (int index = 0; index < features.Count; index++)
                {
                    if (string.Equals(features[index]?.KindKey, "feature", StringComparison.Ordinal))
                    {
                        results.Add(Render(features[index], new ChipFeatureRenderContext(contextKey)));
                    }
                }
            }

            return results.AsReadOnly();
        }
    }
}
