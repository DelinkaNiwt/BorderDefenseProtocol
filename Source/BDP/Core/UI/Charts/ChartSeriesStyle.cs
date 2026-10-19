using UnityEngine;

namespace BDP.Core.UI.Charts
{
    /// <summary>一条图表数据序列的绘制样式。</summary>
    public sealed class ChartSeriesStyle
    {
        /// <summary>线条颜色。</summary>
        public Color LineColor = Color.white;

        /// <summary>线条宽度。</summary>
        public float LineWidth = 1.2f;

        /// <summary>是否绘制虚线。</summary>
        public bool Dashed;

        /// <summary>虚线实线段长度。</summary>
        public float DashLength = 5f;

        /// <summary>虚线空隙长度。</summary>
        public float DashGap = 3f;

        /// <summary>是否绘制填充区域。</summary>
        public bool Filled;

        /// <summary>填充颜色。</summary>
        public Color FillColor = Color.white;

        /// <summary>填充透明度。</summary>
        public float FillOpacity = 0.18f;

        /// <summary>是否绘制数据点。</summary>
        public bool ShowPoints;

        /// <summary>数据点半径。</summary>
        public float PointRadius = 2.8f;

        /// <summary>数据点颜色。</summary>
        public Color PointColor = Color.white;

        /// <summary>是否绘制数据值文字。</summary>
        public bool ShowValues;

        /// <summary>数据值文字颜色。</summary>
        public Color ValueColor = Color.white;

        /// <summary>创建默认序列样式。</summary>
        public static ChartSeriesStyle CreateDefault()
        {
            return new ChartSeriesStyle();
        }

        /// <summary>创建当前样式的独立副本。</summary>
        public ChartSeriesStyle Clone()
        {
            return new ChartSeriesStyle
            {
                LineColor = LineColor,
                LineWidth = LineWidth,
                Dashed = Dashed,
                DashLength = DashLength,
                DashGap = DashGap,
                Filled = Filled,
                FillColor = FillColor,
                FillOpacity = FillOpacity,
                ShowPoints = ShowPoints,
                PointRadius = PointRadius,
                PointColor = PointColor,
                ShowValues = ShowValues,
                ValueColor = ValueColor
            };
        }
    }
}
