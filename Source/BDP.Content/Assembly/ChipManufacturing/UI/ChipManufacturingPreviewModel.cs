using BDP.Content.Assembly;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>制造台中栏当前一次统一详情预览。</summary>
    public sealed class ChipManufacturingPreviewModel
    {
        /// <summary>当前组合解析得到的完整玩家向成品名称。</summary>
        public string ProductLabel { get; set; }

        /// <summary>组合无有效预览时沿用的状态文本。</summary>
        public string StatusText { get; set; }

        /// <summary>当前制造结果与共用图表基准。</summary>
        internal ChipInformationComparisonModel Comparison { get; set; }
    }
}
