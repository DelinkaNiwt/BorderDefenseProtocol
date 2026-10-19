namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息中的玩家可见描述事实。
    /// 描述文本由 Content 侧先按 Def 声明的语言键解析，Core 只保存结果。
    /// </summary>
    public sealed class ChipInformationDescription
    {
        /// <summary>描述条目的稳定来源键。</summary>
        public string SourceKey { get; private set; }

        /// <summary>玩家看到的条目名称。</summary>
        public string DisplayLabel { get; private set; }

        /// <summary>当前语言下的玩家可见描述。</summary>
        public string Text { get; private set; }

        /// <summary>创建描述事实。</summary>
        public ChipInformationDescription(
            string sourceKey,
            string displayLabel,
            string text)
        {
            SourceKey = sourceKey;
            DisplayLabel = displayLabel;
            Text = text;
        }
    }
}
