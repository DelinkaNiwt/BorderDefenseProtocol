using UnityEngine;

namespace BDP.Core.UI.Charts
{
    /// <summary>把数据坐标映射到图表局部坐标的中性视口。</summary>
    public sealed class ChartViewport
    {
        /// <summary>图表局部绘图区。</summary>
        public readonly Rect PlotRect;

        /// <summary>横坐标范围。</summary>
        public readonly ChartRange XRange;

        /// <summary>纵坐标范围。</summary>
        public readonly ChartRange YRange;

        /// <summary>创建图表视口。</summary>
        public ChartViewport(Rect plotRect, ChartRange xRange, ChartRange yRange)
        {
            PlotRect = plotRect;
            XRange = xRange;
            YRange = yRange;
        }

        /// <summary>尝试把数据点映射到图表局部坐标。</summary>
        public bool TryMap(ChartPoint point, out Vector2 mapped)
        {
            mapped = Vector2.zero;
            if (!IsFinite(PlotRect.x)
                || !IsFinite(PlotRect.y)
                || !IsFinite(PlotRect.width)
                || !IsFinite(PlotRect.height)
                || PlotRect.width <= 0f
                || PlotRect.height <= 0f
                || !XRange.IsValid
                || !YRange.IsValid
                || !IsFinite(point.X)
                || !IsFinite(point.Y))
            {
                return false;
            }

            float xRatio = (point.X - XRange.Minimum) / (XRange.Maximum - XRange.Minimum);
            float yRatio = (point.Y - YRange.Minimum) / (YRange.Maximum - YRange.Minimum);
            mapped = new Vector2(
                PlotRect.x + PlotRect.width * xRatio,
                PlotRect.yMax - PlotRect.height * yRatio);
            return true;
        }

        /// <summary>判断浮点数是否可用于绘图。</summary>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
