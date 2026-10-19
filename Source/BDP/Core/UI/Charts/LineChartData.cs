using System.Collections.Generic;

namespace BDP.Core.UI.Charts
{
    /// <summary>二维折线图的一条数据序列。</summary>
    public sealed class LineChartSeries
    {
        /// <summary>序列标签。</summary>
        public readonly string Label;

        /// <summary>按照绘制顺序排列的数据点。</summary>
        public readonly IReadOnlyList<ChartPoint> Points;

        /// <summary>序列绘制样式。</summary>
        public readonly ChartSeriesStyle Style;

        /// <summary>创建折线图数据序列。</summary>
        public LineChartSeries(string label, IReadOnlyList<ChartPoint> points, ChartSeriesStyle style = null)
        {
            Label = label ?? string.Empty;
            Points = points;
            Style = style ?? ChartSeriesStyle.CreateDefault();
        }
    }

    /// <summary>中性二维折线图数据。</summary>
    public sealed class LineChartData
    {
        /// <summary>横坐标范围。</summary>
        public readonly ChartRange XRange;

        /// <summary>纵坐标范围。</summary>
        public readonly ChartRange YRange;

        /// <summary>横坐标刻度。</summary>
        public readonly IReadOnlyList<float> XTicks;

        /// <summary>纵坐标刻度。</summary>
        public readonly IReadOnlyList<float> YTicks;

        /// <summary>图表数据序列集合。</summary>
        public readonly IReadOnlyList<LineChartSeries> Series;

        /// <summary>创建折线图数据。</summary>
        public LineChartData(
            ChartRange xRange,
            ChartRange yRange,
            IReadOnlyList<LineChartSeries> series,
            IReadOnlyList<float> xTicks = null,
            IReadOnlyList<float> yTicks = null)
        {
            XRange = xRange;
            YRange = yRange;
            Series = series;
            XTicks = xTicks;
            YTicks = yTicks;
        }
    }
}
