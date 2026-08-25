using System.Collections.Generic;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>中栏当前一次完整预览。</summary>
    public sealed class ChipManufacturingPreviewModel
    {
        /// <summary>当前完整动态成品名称。</summary>
        public string ProductLabel { get; set; }

        /// <summary>组合无有效预览时显示的状态文本。</summary>
        public string StatusText { get; set; }

        /// <summary>芯片本体规格，同级排列。</summary>
        public List<ChipMetricPreview> Specifications { get; } =
            new List<ChipMetricPreview>();

        /// <summary>武装型修正只在形态之前显示一次。</summary>
        public List<ChipAdjustmentPreview> ArmamentFormAdjustments { get; } =
            new List<ChipAdjustmentPreview>();

        /// <summary>武装型统一提供的共性属性，只在武装型区显示一次。</summary>
        public List<ChipMetricPreview> ArmamentFormMetrics { get; } =
            new List<ChipMetricPreview>();

        /// <summary>按实际动作数量上下排列的形态块。</summary>
        public List<ChipActionFormPreview> ActionForms { get; } =
            new List<ChipActionFormPreview>();
    }

    /// <summary>一个可选择绘制固定标尺条形图的字段。</summary>
    public sealed class ChipMetricPreview
    {
        /// <summary>语言包标签键。</summary>
        public string LabelKey { get; set; }

        /// <summary>字段精确数值文本。</summary>
        public string ValueText { get; set; }

        /// <summary>固定标尺上的 0～1 值。</summary>
        public float NormalizedValue { get; set; }

        /// <summary>当前字段是否适合显示方向明确的条形图。</summary>
        public bool ShowBar { get; set; }

        /// <summary>当前最终值是否受武装型修正。</summary>
        public bool IsModified { get; set; }
    }

    /// <summary>一个动作形态及其同级属性。</summary>
    public sealed class ChipActionFormPreview
    {
        /// <summary>形态显示名称。</summary>
        public string Label { get; set; }

        /// <summary>该形态的动作属性。</summary>
        public List<ChipMetricPreview> Metrics { get; } =
            new List<ChipMetricPreview>();
    }

    /// <summary>武装型的一条绝对或倍率修正。</summary>
    public sealed class ChipAdjustmentPreview
    {
        /// <summary>被修正字段的语言包标签键。</summary>
        public string LabelKey { get; set; }

        /// <summary>使用 → 或 × 表达的修正值。</summary>
        public string OperationText { get; set; }
    }
}
