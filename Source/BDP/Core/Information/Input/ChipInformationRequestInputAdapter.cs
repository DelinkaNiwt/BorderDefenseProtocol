using BDP.Core.Chips;

namespace BDP.Core.Information
{
    /// <summary>把统一请求转换为一般信息规则仍可消费的中性输入。</summary>
    public static class ChipInformationRequestInputAdapter
    {
        /// <summary>优先使用有效配置，制造候选没有有效配置时回退声明配置。</summary>
        public static ChipInformationParseInput Create(ChipInformationRequest request)
        {
            ChipDefinitionConfig definition = request?.EffectiveConfig
                ?? request?.DeclaredConfig;
            return ChipInformationDefinitionInputAdapter.Create(
                definition,
                request?.Identity,
                request?.Runtime ?? ChipInformationRuntimeInputAdapter.CreateStatic(),
                null,
                request?.Descriptions,
                request?.AdditionalBlocks);
        }
    }
}
