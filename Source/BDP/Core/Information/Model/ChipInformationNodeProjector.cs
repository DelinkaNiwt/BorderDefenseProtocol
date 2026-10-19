using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>组装芯片根节点，并补充功能属性的构型来源。</summary>
    internal static class ChipInformationNodeProjector
    {
        /// <summary>上下文树组装用的临时父键；最终根节点树会移除此属性。</summary>
        internal const string ParentContextPropertyKey = "__parent_context";

        /// <summary>用已经统一格式化的节点创建芯片根节点。</summary>
        internal static ChipInformationNode Project(
            ChipInformationIdentity identity,
            IEnumerable<ChipInformationNode> informationNodes,
            IEnumerable<ChipInformationNode> featureNodes,
            IEnumerable<ChipInformationNode> contextNodes,
            IEnumerable<ChipInformationExtensionReference> references,
            IEnumerable<ChipInformationPropertySource> featurePropertySources = null)
        {
            List<ChipInformationProperty> properties = new List<ChipInformationProperty>();
            List<ChipInformationNode> children = new List<ChipInformationNode>();
            if (!string.IsNullOrWhiteSpace(identity?.SourceProfessionKey))
            {
                properties.Add(new ChipInformationProperty(
                    "identity.source_profession",
                    null,
                    ChipInformationValueKind.Enum,
                    ChipInformationValue.FromEnum(identity.SourceProfessionKey),
                    null,
                    null));
            }

            AddRange(children, informationNodes);
            AddFeatureNodes(children, featureNodes, featurePropertySources);
            AddFeatureNodes(children, contextNodes, featurePropertySources);
            AddReferences(children, references);

            return new ChipInformationNode(
                RootKey(identity),
                "chip",
                identity?.DefinitionKey,
                identity?.DisplayLabelKey,
                0,
                properties,
                children);
        }

        /// <summary>用实例键优先确定根节点稳定键。</summary>
        private static string RootKey(ChipInformationIdentity identity)
        {
            return !string.IsNullOrWhiteSpace(identity?.InstanceKey)
                ? identity.InstanceKey
                : identity?.DefinitionKey;
        }

        /// <summary>递归附加同键构型来源并追加功能树。</summary>
        private static void AddFeatureNodes(
            ICollection<ChipInformationNode> target,
            IEnumerable<ChipInformationNode> source,
            IEnumerable<ChipInformationPropertySource> propertySources)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (ChipInformationNode node in source)
            {
                if (node != null)
                {
                    target.Add(WithFeatureSources(node, propertySources));
                }
            }
        }

        /// <summary>只为功能节点及其子组属性补充构型原始来源。</summary>
        private static ChipInformationNode WithFeatureSources(
            ChipInformationNode node,
            IEnumerable<ChipInformationPropertySource> sources)
        {
            if (node == null)
            {
                return null;
            }

            bool isFeatureNode = IsFeatureKind(node.KindKey);
            List<ChipInformationProperty> properties = new List<ChipInformationProperty>();
            if (node.Properties != null)
            {
                for (int index = 0; index < node.Properties.Count; index++)
                {
                    ChipInformationProperty property = node.Properties[index];
                    properties.Add(isFeatureNode
                        ? AddPropertySources(property, sources)
                        : property);
                }
            }

            List<ChipInformationNode> children = new List<ChipInformationNode>();
            if (node.Children != null)
            {
                for (int index = 0; index < node.Children.Count; index++)
                {
                    children.Add(WithFeatureSources(node.Children[index], sources));
                }
            }

            return new ChipInformationNode(
                node.Key,
                node.KindKey,
                node.DefinitionKey,
                node.LabelKey,
                node.Order,
                properties,
                children);
        }

        /// <summary>把匹配属性键的构型来源并入该属性的来源链。</summary>
        private static ChipInformationProperty AddPropertySources(
            ChipInformationProperty property,
            IEnumerable<ChipInformationPropertySource> sources)
        {
            if (property == null || sources == null)
            {
                return property;
            }

            List<ChipInformationProvenance> provenance =
                new List<ChipInformationProvenance>();
            AddRange(provenance, property.Sources);
            foreach (ChipInformationPropertySource source in sources)
            {
                if (source?.Provenance != null
                    && string.Equals(source.PropertyKey, property.Key, StringComparison.Ordinal)
                    && IsApplicableSource(source, property.Sources))
                {
                    provenance.Add(source.Provenance);
                }
            }

            return new ChipInformationProperty(
                property.Key,
                property.LabelKey,
                property.ValueKind,
                property.Value,
                property.UnitKey,
                provenance);
        }

        /// <summary>按已裁定的属性来源类别限制构型字段的关联。</summary>
        private static bool IsApplicableSource(
            ChipInformationPropertySource source,
            IReadOnlyList<ChipInformationProvenance> propertySources)
        {
            if (source?.ApplicableSourceKinds == null || source.ApplicableSourceKinds.Count == 0)
            {
                return true;
            }

            if (propertySources == null || propertySources.Count == 0)
            {
                return false;
            }

            for (int sourceIndex = 0; sourceIndex < propertySources.Count; sourceIndex++)
            {
                ChipInformationProvenance propertySource = propertySources[sourceIndex];
                if (propertySource == null)
                {
                    continue;
                }

                for (int kindIndex = 0; kindIndex < source.ApplicableSourceKinds.Count; kindIndex++)
                {
                    if (propertySource.SourceKind == source.ApplicableSourceKinds[kindIndex])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>把中性外部引用转换为关系节点。</summary>
        internal static ChipInformationNode CreateRelationNode(
            ChipInformationExtensionReference reference)
        {
            if (reference == null)
            {
                return null;
            }

            List<ChipInformationProperty> properties = new List<ChipInformationProperty>();
            AddIfPresent(properties, "provider", reference.ProviderKey);
            AddIfPresent(properties, "reference_kind", reference.ReferenceKindKey);
            AddIfPresent(properties, "reference_key", reference.ReferenceKey);
            AddIfPresent(properties, "role", reference.RelationRoleKey);
            return new ChipInformationNode(
                "relation/" + reference.ProviderKey + "/"
                    + reference.ReferenceKindKey + "/" + reference.ReferenceKey + "/"
                    + (reference.RelationRoleKey ?? "default"),
                "relation",
                null,
                null,
                0,
                properties,
                null);
        }

        /// <summary>递归添加关系节点。</summary>
        private static void AddReferences(
            ICollection<ChipInformationNode> target,
            IEnumerable<ChipInformationExtensionReference> references)
        {
            if (target == null || references == null)
            {
                return;
            }

            foreach (ChipInformationExtensionReference reference in references)
            {
                ChipInformationNode relation = CreateRelationNode(reference);
                if (relation != null)
                {
                    target.Add(relation);
                }
            }
        }

        /// <summary>仅在关系属性有内容时写入属性。</summary>
        private static void AddIfPresent(
            ICollection<ChipInformationProperty> target,
            string key,
            string value)
        {
            if (target != null && !string.IsNullOrWhiteSpace(value))
            {
                target.Add(new ChipInformationProperty(
                    key,
                    null,
                    ChipInformationValueKind.Text,
                    ChipInformationValue.FromText(value),
                    null,
                    null));
            }
        }

        /// <summary>识别功能节点及功能信息分组。</summary>
        private static bool IsFeatureKind(string kindKey)
        {
            return string.Equals(kindKey, "feature", StringComparison.Ordinal)
                || string.Equals(kindKey, "feature_group", StringComparison.Ordinal)
                || string.Equals(kindKey, "feature_subgroup", StringComparison.Ordinal);
        }

        /// <summary>安全追加非空集合项。</summary>
        private static void AddRange<T>(ICollection<T> target, IEnumerable<T> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (T item in source)
            {
                if (item != null)
                {
                    target.Add(item);
                }
            }
        }
    }
}
