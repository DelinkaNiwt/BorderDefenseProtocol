using System;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息模型的统一读取服务，供制造、装配与表达消费者共用。
    /// </summary>
    public sealed class ChipInformationService : IChipInformationReader
    {
        /// <summary>实际解析器。</summary>
        private readonly ChipInformationParser parser;

        /// <summary>创建自定义规则集的信息服务。</summary>
        public ChipInformationService(ChipInformationRuleRegistry registry)
        {
            parser = new ChipInformationParser(
                registry ?? throw new ArgumentNullException(nameof(registry)));
        }

        /// <summary>读取一份统一芯片信息模型。</summary>
        public ChipInformationModel Read(ChipInformationParseInput input)
        {
            return parser.Parse(input);
        }

        /// <summary>按制造台、装配台和运行实例共用请求读取语义模型。</summary>
        public ChipInformationModel Read(ChipInformationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return parser.Parse(
                ChipInformationRequestInputAdapter.Create(request),
                request);
        }

        /// <summary>创建带有 Core 通用规则的默认服务。</summary>
        internal static ChipInformationService CreateDefault()
        {
            ChipInformationRuleRegistry registry = new ChipInformationRuleRegistry();
            registry.Register(new ProfileInformationRule());
            registry.Register(new LoadoutInformationRule());
            registry.Register(new TrionInformationRule());
            registry.Register(new ActivationRequirementInformationRule());
            registry.Register(new VariantProvenanceRule());
            // StructuredBlocks 现在只承载描述、武装构型等一般信息，不再承载技术功能倾倒。
            registry.Register(new StructuredFactBlockInformationRule());
            return new ChipInformationService(registry);
        }
    }
}
