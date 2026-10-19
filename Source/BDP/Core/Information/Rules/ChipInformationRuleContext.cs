namespace BDP.Core.Information
{
    /// <summary>
    /// 通用信息规则读取的上下文。
    /// </summary>
    public sealed class ChipInformationRuleContext
    {
        /// <summary>当前解析输入。</summary>
        public ChipInformationParseInput Input { get; private set; }

        /// <summary>创建规则上下文。</summary>
        public ChipInformationRuleContext(ChipInformationParseInput input)
        {
            Input = input;
        }
    }
}
