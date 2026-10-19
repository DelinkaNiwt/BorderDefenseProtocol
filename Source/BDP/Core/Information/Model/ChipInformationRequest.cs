using BDP.Core.Chips;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 制造台、装配台和其它消费者共用的芯片信息读取请求。
    /// </summary>
    public sealed class ChipInformationRequest
    {
        /// <summary>芯片身份元数据。</summary>
        public ChipInformationIdentity Identity { get; private set; }

        /// <summary>作者或制造预设声明配置。</summary>
        public ChipDefinitionConfig DeclaredConfig { get; private set; }

        /// <summary>制造、武装构型等处理后的有效配置。</summary>
        public ChipDefinitionConfig EffectiveConfig { get; private set; }

        /// <summary>可选运行时状态快照。</summary>
        public ChipInformationRuntimeState Runtime { get; private set; }

        /// <summary>当前活动形态键。</summary>
        public string CurrentModeKey { get; private set; }

        /// <summary>当前活动姿态键。</summary>
        public string CurrentStanceKey { get; private set; }

        /// <summary>来自制造预设、构型或实例的语言化描述。</summary>
        public IReadOnlyList<ChipInformationDescription> Descriptions { get; private set; }

        /// <summary>一般信息层的额外结构块。</summary>
        public IReadOnlyList<ChipInformationFactBlock> AdditionalBlocks { get; private set; }

        /// <summary>由信息扩展提供的外部目标引用。</summary>
        public IReadOnlyList<ChipInformationExtensionReference> ExtensionReferences { get; private set; }

        /// <summary>可关联到功能属性的原始来源值。</summary>
        public IReadOnlyList<ChipInformationPropertySource> FeaturePropertySources { get; private set; }

        /// <summary>创建统一信息请求。</summary>
        public ChipInformationRequest(
            ChipInformationIdentity identity,
            ChipDefinitionConfig declaredConfig,
            ChipDefinitionConfig effectiveConfig,
            ChipInformationRuntimeState runtime = null,
            string currentModeKey = null,
            string currentStanceKey = null,
            IEnumerable<ChipInformationDescription> descriptions = null,
            IEnumerable<ChipInformationFactBlock> additionalBlocks = null,
            IEnumerable<ChipInformationExtensionReference> extensionReferences = null,
            IEnumerable<ChipInformationPropertySource> featurePropertySources = null)
        {
            Identity = identity;
            DeclaredConfig = declaredConfig;
            EffectiveConfig = effectiveConfig;
            Runtime = runtime ?? new ChipInformationRuntimeState();
            CurrentModeKey = currentModeKey;
            CurrentStanceKey = currentStanceKey;
            Descriptions = Copy(descriptions);
            AdditionalBlocks = Copy(additionalBlocks);
            ExtensionReferences = Copy(extensionReferences);
            FeaturePropertySources = Copy(featurePropertySources);
        }

        /// <summary>复制描述列表为只读集合。</summary>
        private static IReadOnlyList<ChipInformationDescription> Copy(
            IEnumerable<ChipInformationDescription> source)
        {
            List<ChipInformationDescription> result =
                new List<ChipInformationDescription>();
            if (source != null)
            {
                foreach (ChipInformationDescription item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>复制一般信息块列表为只读集合。</summary>
        private static IReadOnlyList<ChipInformationFactBlock> Copy(
            IEnumerable<ChipInformationFactBlock> source)
        {
            List<ChipInformationFactBlock> result =
                new List<ChipInformationFactBlock>();
            if (source != null)
            {
                foreach (ChipInformationFactBlock item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>复制扩展引用列表为只读集合。</summary>
        private static IReadOnlyList<ChipInformationExtensionReference> Copy(
            IEnumerable<ChipInformationExtensionReference> source)
        {
            List<ChipInformationExtensionReference> result =
                new List<ChipInformationExtensionReference>();
            if (source != null)
            {
                foreach (ChipInformationExtensionReference item in source)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>复制功能属性来源集合为只读列表。</summary>
        private static IReadOnlyList<ChipInformationPropertySource> Copy(
            IEnumerable<ChipInformationPropertySource> source)
        {
            List<ChipInformationPropertySource> result =
                new List<ChipInformationPropertySource>();
            if (source != null)
            {
                foreach (ChipInformationPropertySource item in source)
                {
                    if (item?.Provenance != null
                        && !string.IsNullOrWhiteSpace(item.PropertyKey))
                    {
                        result.Add(item);
                    }
                }
            }

            return result.AsReadOnly();
        }
    }
}
