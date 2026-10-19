using System.Collections.Generic;

namespace BDP.Core.UI.Charts
{
    /// <summary>雷达图单条轴的玩家向标签和数值范围。</summary>
    public sealed class RadarChartAxis
    {
        /// <summary>轴标签。</summary>
        public readonly string Label;

        /// <summary>轴最小值。</summary>
        public readonly float Minimum;

        /// <summary>轴最大值。</summary>
        public readonly float Maximum;

        /// <summary>创建雷达图轴。</summary>
        public RadarChartAxis(string label, float minimum, float maximum)
        {
            Label = label ?? string.Empty;
            Minimum = minimum;
            Maximum = maximum;
        }
    }

    /// <summary>雷达图的一条数据序列。</summary>
    public sealed class RadarChartSeries
    {
        /// <summary>序列标签。</summary>
        public readonly string Label;

        /// <summary>按照轴顺序排列的数值。</summary>
        public readonly IReadOnlyList<float> Values;

        /// <summary>序列绘制样式。</summary>
        public readonly ChartSeriesStyle Style;

        /// <summary>可选的逐轴可见标记；缺失值会中断相邻轴线并禁止填充。</summary>
        public readonly IReadOnlyList<bool> VisibleAxes;

        /// <summary>创建雷达图数据序列。</summary>
        public RadarChartSeries(string label, IReadOnlyList<float> values, ChartSeriesStyle style = null)
            : this(label, values, style, null)
        {
        }

        /// <summary>创建可逐轴标记缺失值的雷达图数据序列。</summary>
        public RadarChartSeries(
            string label,
            IReadOnlyList<float> values,
            ChartSeriesStyle style,
            IReadOnlyList<bool> visibleAxes)
        {
            Label = label ?? string.Empty;
            Values = values;
            Style = style ?? ChartSeriesStyle.CreateDefault();
            VisibleAxes = visibleAxes;
        }
    }

    /// <summary>中性雷达图数据。</summary>
    public sealed class RadarChartData
    {
        /// <summary>图表轴集合。</summary>
        public readonly IReadOnlyList<RadarChartAxis> Axes;

        /// <summary>图表数据序列集合。</summary>
        public readonly IReadOnlyList<RadarChartSeries> Series;

        /// <summary>创建雷达图数据。</summary>
        public RadarChartData(
            IReadOnlyList<RadarChartAxis> axes,
            IReadOnlyList<RadarChartSeries> series)
        {
            Axes = axes;
            Series = series;
        }
    }
}
