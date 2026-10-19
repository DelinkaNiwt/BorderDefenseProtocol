using System;
using UnityEngine;
using Verse;

namespace BDP.Core.UI.Charts
{
    /// <summary>中性图表的公共视觉样式。</summary>
    public sealed class ChartStyle
    {
        /// <summary>图表背景色。</summary>
        public Color BackgroundColor = new Color(0.03f, 0.09f, 0.11f, 0.95f);

        /// <summary>图表边框色。</summary>
        public Color BorderColor = new Color(0.32f, 0.50f, 0.53f, 0.72f);

        /// <summary>网格色。</summary>
        public Color GridColor = new Color(0.29f, 0.59f, 0.61f, 0.50f);

        /// <summary>轴线色。</summary>
        public Color AxisColor = new Color(0.43f, 0.61f, 0.63f, 0.90f);

        /// <summary>标签颜色。</summary>
        public Color LabelColor = Color.white;

        /// <summary>数值颜色。</summary>
        public Color ValueColor = Color.white;

        /// <summary>标签字体。</summary>
        public GameFont LabelFont = GameFont.Tiny;

        /// <summary>标签文字对齐方式。</summary>
        public TextAnchor LabelAnchor = TextAnchor.MiddleCenter;

        /// <summary>网格层数。</summary>
        public int GridLevels = 4;

        /// <summary>折线绘图区左边距。</summary>
        public float PlotLeft = 40f;

        /// <summary>折线绘图区右边距。</summary>
        public float PlotRight = 8f;

        /// <summary>折线绘图区上边距。</summary>
        public float PlotTop = 24f;

        /// <summary>折线绘图区下边距。</summary>
        public float PlotBottom = 24f;

        /// <summary>折线图绘图区外允许刻度文字延伸的下边距。</summary>
        public float LineChartBottomOverflow;

        /// <summary>是否绘制横坐标刻度对应的竖向网格线。</summary>
        public bool ShowXGridLines = true;

        /// <summary>横坐标刻度文字的布局宽度。</summary>
        public float XTickLabelWidth = 40f;

        /// <summary>横坐标刻度文字的布局高度。</summary>
        public float XTickLabelHeight = 16f;

        /// <summary>纵坐标刻度文字的布局宽度。</summary>
        public float YTickLabelWidth = 36f;

        /// <summary>纵坐标刻度文字的布局高度。</summary>
        public float YTickLabelHeight = 16f;

        /// <summary>雷达图是否把首个数值序列的值显示在轴标签下方。</summary>
        public bool RadarAxisValuesBelowLabels;

        /// <summary>雷达轴名称和值的行框间距；负值可收紧原生字体自带留白，主体几何不变。</summary>
        public float RadarAxisLabelValueGap = 1f;

        /// <summary>雷达图的几何绘图区高度；为零时使用整个图表高度。</summary>
        public float RadarGeometryHeight;

        /// <summary>是否在基准系列和当前系列之间显示逐轴下降差值虚线。</summary>
        public bool ShowRadarDecreaseSegments;

        /// <summary>雷达逐轴下降差值线的颜色。</summary>
        public Color RadarDecreaseColor = Color.gray;

        /// <summary>雷达逐轴下降差值线的宽度。</summary>
        public float RadarDecreaseLineWidth = 0.9f;

        /// <summary>雷达轴数值的按轴格式化器；为空时使用通用数值格式。</summary>
        public Func<int, float, string> RadarAxisValueFormatter;

        /// <summary>雷达轴数值颜色的按轴选择器；为空时使用数值文字颜色。</summary>
        public Func<int, float, Color> RadarAxisValueColorProvider;

        /// <summary>图形到绘图区边界的内边距。</summary>
        public float PlotPadding = 8f;

        /// <summary>数值标签格式化器；为空时使用通用短数值格式。</summary>
        public Func<float, string> ValueFormatter;

        /// <summary>横坐标刻度格式化器；为空时沿用通用数值格式。</summary>
        public Func<float, string> XValueFormatter;

        /// <summary>创建默认样式。</summary>
        public static ChartStyle CreateDefault()
        {
            return new ChartStyle();
        }

        /// <summary>创建当前样式的独立副本。</summary>
        public ChartStyle Clone()
        {
            return new ChartStyle
            {
                BackgroundColor = BackgroundColor,
                BorderColor = BorderColor,
                GridColor = GridColor,
                AxisColor = AxisColor,
                LabelColor = LabelColor,
                ValueColor = ValueColor,
                LabelFont = LabelFont,
                LabelAnchor = LabelAnchor,
                GridLevels = GridLevels,
                PlotLeft = PlotLeft,
                PlotRight = PlotRight,
                PlotTop = PlotTop,
                PlotBottom = PlotBottom,
                LineChartBottomOverflow = LineChartBottomOverflow,
                ShowXGridLines = ShowXGridLines,
                XTickLabelWidth = XTickLabelWidth,
                XTickLabelHeight = XTickLabelHeight,
                YTickLabelWidth = YTickLabelWidth,
                YTickLabelHeight = YTickLabelHeight,
                RadarAxisValuesBelowLabels = RadarAxisValuesBelowLabels,
                RadarAxisLabelValueGap = RadarAxisLabelValueGap,
                RadarGeometryHeight = RadarGeometryHeight,
                ShowRadarDecreaseSegments = ShowRadarDecreaseSegments,
                RadarDecreaseColor = RadarDecreaseColor,
                RadarDecreaseLineWidth = RadarDecreaseLineWidth,
                RadarAxisValueFormatter = RadarAxisValueFormatter,
                RadarAxisValueColorProvider = RadarAxisValueColorProvider,
                PlotPadding = PlotPadding,
                ValueFormatter = ValueFormatter,
                XValueFormatter = XValueFormatter
            };
        }

        /// <summary>把数值格式化为玩家可读的短文本。</summary>
        public string FormatValue(float value)
        {
            return ValueFormatter != null
                ? ValueFormatter(value)
                : value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>把横坐标刻度格式化为玩家可读的短文本。</summary>
        public string FormatXValue(float value)
        {
            return XValueFormatter != null
                ? XValueFormatter(value)
                : FormatValue(value);
        }
    }
}
