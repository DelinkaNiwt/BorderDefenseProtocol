using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息解析器接收的中性输入。
    /// </summary>
    public sealed class ChipInformationParseInput
    {
        /// <summary>芯片身份，仅用于结果追踪。</summary>
        public ChipInformationIdentity Identity { get; private set; }

        /// <summary>芯片定义和表达事实。</summary>
        public ChipInformationFacts Facts { get; private set; }

        /// <summary>可选运行时状态。</summary>
        public ChipInformationRuntimeState Runtime { get; private set; }

        /// <summary>外部适配器提供的基础来源记录。</summary>
        public IReadOnlyList<ChipInformationProvenance> ProvenanceSources { get; private set; }

        /// <summary>创建解析输入。</summary>
        public ChipInformationParseInput(
            ChipInformationIdentity identity,
            ChipInformationFacts facts,
            ChipInformationRuntimeState runtime,
            IEnumerable<ChipInformationProvenance> provenanceSources)
        {
            Identity = identity;
            Facts = facts ?? new ChipInformationFacts();
            Runtime = runtime ?? new ChipInformationRuntimeState();
            List<ChipInformationProvenance> result = new List<ChipInformationProvenance>();
            if (provenanceSources != null)
            {
                foreach (ChipInformationProvenance source in provenanceSources)
                {
                    if (source != null)
                    {
                        result.Add(source);
                    }
                }
            }

            ProvenanceSources = result.AsReadOnly();
        }
    }
}
