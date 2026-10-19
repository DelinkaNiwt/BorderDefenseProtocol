using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.UI.Charts
{
    /// <summary>中性静态雷达图渲染器。</summary>
    public static class RadarChartRenderer
    {
        /// <summary>外围文字到图框的固定内边距。</summary>
        private const float OutsideLabelPadding = 4f;

        /// <summary>外围文字到雷达网格的固定间距。</summary>
        private const float OutsideLabelGap = 5f;

        /// <summary>按实际轴名称和字体测量完整外围文字所需宽度，不压缩既定几何半径。</summary>
        public static float MeasureOutsideAxisWidth(IReadOnlyList<RadarChartAxis> axes, GameFont font, float geometryHeight)
        {
            MeasureOutsideSideSpace(axes, font, geometryHeight * 0.40f, out float left, out float right);
            return left + right;
        }

        /// <summary>测量左右轴名称与几何边界分别占用的空间；恢复原版当前字体。</summary>
        private static void MeasureOutsideSideSpace(IReadOnlyList<RadarChartAxis> axes, GameFont font, float radius,
            out float left, out float right)
        {
            float leftLabel = 0f, rightLabel = 0f, leftExtent = 0f, rightExtent = 0f;
            GameFont previousFont = Text.Font;
            try
            {
                Text.Font = font;
                for (int index = 0; index < axes.Count; index++)
                {
                    float x = RadarPoint(Vector2.zero, radius, axes.Count, index, 1f).x;
                    if (Mathf.Abs(x) < 0.1f) continue;
                    float width = Mathf.Ceil(Text.CalcSize(axes[index]?.Label ?? string.Empty).x);
                    if (x < 0f) { leftLabel = Mathf.Max(leftLabel, width); leftExtent = Mathf.Max(leftExtent, -x); }
                    else { rightLabel = Mathf.Max(rightLabel, width); rightExtent = Mathf.Max(rightExtent, x); }
                }
            }
            finally { Text.Font = previousFont; }
            left = leftExtent + leftLabel + OutsideLabelPadding + OutsideLabelGap;
            right = rightExtent + rightLabel + OutsideLabelPadding + OutsideLabelGap;
        }

        /// <summary>绘制一张雷达图。</summary>
        public static void Draw(
            Rect chartRect,
            Rect visibleRect,
            RadarChartData data,
            ChartStyle style = null)
        {
            style = style ?? ChartStyle.CreateDefault();
            if (data == null || data.Axes == null || data.Axes.Count < 3)
            {
                return;
            }

            Rect visible = Intersect(chartRect, visibleRect.width > 0f && visibleRect.height > 0f ? visibleRect : chartRect);
            if (visible.width <= 0f || visible.height <= 0f)
            {
                return;
            }

            Widgets.DrawBoxSolidWithOutline(visible, style.BackgroundColor, style.BorderColor);
            ChartCanvas canvas = new ChartCanvas(chartRect, visible);
            canvas.Draw(context =>
            {
                float width = chartRect.width;
                float height = style.RadarGeometryHeight > 0f
                    ? Mathf.Min(chartRect.height, style.RadarGeometryHeight)
                    : chartRect.height;
                // 外围名称与值需要独立上下留白，几何尺寸仍由原有绘图区高度决定。
                Vector2 center = new Vector2(width * 0.5f,
                    style.RadarAxisValuesBelowLabels ? chartRect.height * 0.5f : height * 0.52f);
                float radius = Mathf.Max(1f, Mathf.Min(width * 0.32f, height * 0.40f));
                if (style.RadarAxisValuesBelowLabels)
                {
                    // 左右名称长度可能不同，只在必要时移动中心，为完整文字让位；主体半径保持不变。
                    MeasureOutsideSideSpace(data.Axes, style.LabelFont, radius, out float left, out float right);
                    if (left + right <= width) center.x = Mathf.Clamp(center.x, left, width - right);
                }
                int count = data.Axes.Count;
                int gridLevels = Mathf.Max(1, style.GridLevels);
                List<Vector2> radarBoundary = BuildRing(center, radius, count);

                for (int level = 1; level <= gridLevels; level++)
                {
                    context.DrawPolyline(
                        BuildRing(center, radius * level / gridLevels, count),
                        style.GridColor,
                        0.8f);
                }

                for (int index = 0; index < count; index++)
                {
                    context.DrawLine(
                        center,
                        RadarPoint(center, radius, count, index, 1f),
                        style.GridColor,
                        0.8f);
                }

                if (data.Series != null)
                {
                    for (int seriesIndex = 0; seriesIndex < data.Series.Count; seriesIndex++)
                    {
                        RadarChartSeries series = data.Series[seriesIndex];
                        if (series == null || series.Values == null || series.Values.Count < count)
                        {
                            continue;
                        }

                        List<Vector2> points = BuildSeriesPoints(center, radius, data.Axes, series.Values);
                        ChartSeriesStyle seriesStyle = series.Style ?? ChartSeriesStyle.CreateDefault();
                        bool complete = HasCompleteSeries(series, count);
                        if (seriesStyle.Filled && complete)
                        {
                            Color fill = seriesStyle.FillColor;
                            fill.a = Mathf.Clamp01(seriesStyle.FillOpacity);
                            context.DrawFilledPolygon(center, points, fill, radarBoundary);
                        }

                        DrawSeriesLines(context, points, series, seriesStyle, radarBoundary, count);
                        if (!style.RadarAxisValuesBelowLabels)
                        {
                            DrawAxisValues(context, center, radius, data.Axes, series, seriesStyle, style);
                        }
                    }
                }

                if (style.ShowRadarDecreaseSegments)
                {
                    DrawRadarDecreaseSegments(context, center, radius, data, style, radarBoundary);
                }

                DrawAxisLabels(
                    context,
                    center,
                    radius,
                    data.Axes,
                    data.Series,
                    style,
                    chartRect.width,
                    chartRect.height);
            });
        }

        /// <summary>判断序列是否在每条轴上都具有有效值。</summary>
        private static bool HasCompleteSeries(RadarChartSeries series, int axisCount)
        {
            if (series?.VisibleAxes == null)
            {
                return true;
            }

            for (int index = 0; index < axisCount; index++)
            {
                if (index >= series.VisibleAxes.Count || !series.VisibleAxes[index])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>绘制闭合完整序列，或只绘制两端数据均存在的相邻轴段。</summary>
        private static void DrawSeriesLines(
            ChartDrawingContext context,
            IReadOnlyList<Vector2> points,
            RadarChartSeries series,
            ChartSeriesStyle seriesStyle,
            IReadOnlyList<Vector2> radarBoundary,
            int axisCount)
        {
            if (HasCompleteSeries(series, axisCount))
            {
                context.DrawPolyline(points, seriesStyle, radarBoundary);
                return;
            }

            if (series?.VisibleAxes == null || seriesStyle.LineWidth <= 0f)
            {
                return;
            }

            for (int index = 0; index < axisCount; index++)
            {
                int next = (index + 1) % axisCount;
                if (index >= series.VisibleAxes.Count
                    || next >= series.VisibleAxes.Count
                    || !series.VisibleAxes[index]
                    || !series.VisibleAxes[next])
                {
                    continue;
                }

                context.DrawPolyline(
                    new[] { points[index], points[next] },
                    seriesStyle,
                    radarBoundary);
            }
        }

        /// <summary>绘制基准值高于当前值的雷达轴差值段。</summary>
        private static void DrawRadarDecreaseSegments(
            ChartDrawingContext context,
            Vector2 center,
            float radius,
            RadarChartData data,
            ChartStyle style,
            IReadOnlyList<Vector2> radarBoundary)
        {
            RadarChartSeries baseline = null;
            RadarChartSeries current = null;
            if (data.Series != null)
            {
                for (int index = 0; index < data.Series.Count; index++)
                {
                    RadarChartSeries series = data.Series[index];
                    if (series?.Style == null)
                    {
                        continue;
                    }

                    if (baseline == null && series.Style.Dashed)
                    {
                        baseline = series;
                    }

                    if (current == null && series.Style.ShowValues)
                    {
                        current = series;
                    }
                }
            }

            if (baseline?.Values == null || current?.Values == null)
            {
                return;
            }

            int axisCount = data.Axes.Count;
            ChartSeriesStyle differenceStyle = new ChartSeriesStyle
            {
                LineColor = style.RadarDecreaseColor,
                LineWidth = style.RadarDecreaseLineWidth,
                Dashed = true
            };
            for (int index = 0; index < axisCount; index++)
            {
                if (index >= baseline.Values.Count
                    || index >= current.Values.Count
                    || !IsAxisVisible(baseline, index)
                    || !IsAxisVisible(current, index)
                    || current.Values[index] >= baseline.Values[index])
                {
                    continue;
                }

                Vector2 from = RadarPoint(
                    center,
                    radius,
                    axisCount,
                    index,
                    GetAxisRatio(data.Axes[index], baseline.Values[index]));
                Vector2 to = RadarPoint(
                    center,
                    radius,
                    axisCount,
                    index,
                    GetAxisRatio(data.Axes[index], current.Values[index]));
                context.DrawPolyline(new[] { from, to }, differenceStyle, radarBoundary);
            }
        }

        /// <summary>判断序列在指定轴上是否有值。</summary>
        private static bool IsAxisVisible(RadarChartSeries series, int index)
        {
            return series?.VisibleAxes == null
                || (index < series.VisibleAxes.Count && series.VisibleAxes[index]);
        }

        /// <summary>把轴数值映射到零到一的雷达半径比例。</summary>
        private static float GetAxisRatio(RadarChartAxis axis, float value)
        {
            return axis != null && axis.Maximum > axis.Minimum
                ? (value - axis.Minimum) / (axis.Maximum - axis.Minimum)
                : 0f;
        }

        /// <summary>绘制雷达轴标签。</summary>
        private static void DrawAxisLabels(
            ChartDrawingContext context,
            Vector2 center,
            float radius,
            IReadOnlyList<RadarChartAxis> axes,
            IReadOnlyList<RadarChartSeries> series,
            ChartStyle style,
            float width,
            float height)
        {
            if (style.RadarAxisValuesBelowLabels)
            {
                DrawOutsideAxisLabels(context, center, radius, axes, series, style, width, height);
                return;
            }
            for (int index = 0; index < axes.Count; index++)
            {
                RadarChartAxis axis = axes[index];
                Vector2 point = RadarPoint(center, radius, axes.Count, index, 1.18f);
                float labelHeight = 18f;
                float labelWidth = Mathf.Min(120f, Mathf.Max(52f, width * 0.30f));
                float x = Mathf.Clamp(
                    point.x - labelWidth * 0.5f,
                    0f,
                    Mathf.Max(0f, width - labelWidth));
                float y = Mathf.Clamp(point.y - labelHeight, 0f, Mathf.Max(0f, height - labelHeight));
                context.DrawText(new Rect(x, y, labelWidth, labelHeight), axis?.Label, style.LabelFont, style.LabelColor, style.LabelAnchor);
            }
        }

        /// <summary>把名称和值放到完整网格外侧；按真实字体测量长标签，避免缩进网格或截断。</summary>
        private static void DrawOutsideAxisLabels(
            ChartDrawingContext context, Vector2 center, float radius,
            IReadOnlyList<RadarChartAxis> axes, IReadOnlyList<RadarChartSeries> series,
            ChartStyle style, float width, float height)
        {
            IReadOnlyList<float> axisValues = FindAxisLabelValues(series, true);
            float leftEdge = center.x, rightEdge = center.x;
            for (int index = 0; index < axes.Count; index++)
            {
                Vector2 vertex = RadarPoint(center, radius, axes.Count, index, 1f);
                leftEdge = Mathf.Min(leftEdge, vertex.x);
                rightEdge = Mathf.Max(rightEdge, vertex.x);
            }
            const float padding = OutsideLabelPadding, gap = OutsideLabelGap;
            for (int index = 0; index < axes.Count; index++)
            {
                Vector2 point = RadarPoint(center, radius, axes.Count, index, 1f);
                bool vertical = Mathf.Abs(point.x - center.x) < 0.1f;
                float x = vertical ? padding : point.x < center.x ? padding : rightEdge + gap;
                float labelWidth = Mathf.Max(1f, vertical ? width - 2f * padding
                    : point.x < center.x ? leftEdge - gap - padding : width - padding - x);
                float labelHeight = MeasureTextHeight(axes[index]?.Label, style.LabelFont, labelWidth);
                bool hasValue = axisValues != null && index < axisValues.Count;
                string valueLabel = hasValue
                    ? style.RadarAxisValueFormatter != null
                        ? style.RadarAxisValueFormatter(index, axisValues[index])
                        : style.FormatValue(axisValues[index])
                    : string.Empty;
                float valueHeight = hasValue ? MeasureTextHeight(valueLabel, style.LabelFont, labelWidth) : 0f;
                float valueGap = style.RadarAxisLabelValueGap;
                float blockHeight = labelHeight + (hasValue ? valueGap + valueHeight : 0f);
                float y = vertical
                    ? point.y < center.y ? point.y - gap - blockHeight : point.y + gap
                    : point.y - blockHeight * 0.5f;
                y = Mathf.Clamp(y, padding, Mathf.Max(padding, height - blockHeight - padding));
                context.DrawText(new Rect(x, y, labelWidth, labelHeight), axes[index]?.Label,
                    style.LabelFont, style.LabelColor, TextAnchor.MiddleCenter);
                if (hasValue)
                {
                    Color valueColor = style.RadarAxisValueColorProvider != null
                        ? style.RadarAxisValueColorProvider(index, axisValues[index])
                        : style.ValueColor;
                    context.DrawText(
                        new Rect(x, y + labelHeight + valueGap, labelWidth, valueHeight),
                        valueLabel,
                        style.LabelFont,
                        valueColor,
                        TextAnchor.MiddleCenter);
                }
            }
        }

        /// <summary>用实际可用宽度和游戏字体测量完整标签高度并恢复原字体。</summary>
        private static float MeasureTextHeight(string value, GameFont font, float width)
        {
            GameFont previousFont = Text.Font;
            try
            {
                Text.Font = font;
                return Mathf.Max(18f, Text.CalcHeight(value ?? string.Empty, width));
            }
            finally
            {
                Text.Font = previousFont;
            }
        }

        /// <summary>查找用于轴旁当前值文字的首个可见值序列。</summary>
        private static IReadOnlyList<float> FindAxisLabelValues(
            IReadOnlyList<RadarChartSeries> series,
            bool showBelowLabels)
        {
            if (!showBelowLabels || series == null)
            {
                return null;
            }

            for (int index = 0; index < series.Count; index++)
            {
                RadarChartSeries item = series[index];
                if (item?.Style != null
                    && item.Style.ShowValues
                    && item.Values != null
                    && HasCompleteSeries(item, item.Values.Count))
                {
                    return item.Values;
                }
            }

            return null;
        }

        /// <summary>绘制各序列在轴外侧的当前数值。</summary>
        private static void DrawAxisValues(
            ChartDrawingContext context,
            Vector2 center,
            float radius,
            IReadOnlyList<RadarChartAxis> axes,
            RadarChartSeries series,
            ChartSeriesStyle seriesStyle,
            ChartStyle style)
        {
            if (!seriesStyle.ShowValues)
            {
                return;
            }

            for (int index = 0; index < axes.Count; index++)
            {
                if (!IsAxisVisible(series, index) || index >= series.Values.Count)
                {
                    continue;
                }

                Vector2 point = RadarPoint(center, radius, axes.Count, index, 1.03f);
                context.DrawText(
                    new Rect(point.x - 36f, point.y + 2f, 72f, 16f),
                    style.FormatValue(series.Values[index]),
                    style.LabelFont,
                    seriesStyle.ValueColor,
                    TextAnchor.MiddleCenter);
            }
        }

        /// <summary>构建闭合雷达网格环。</summary>
        private static List<Vector2> BuildRing(Vector2 center, float radius, int count)
        {
            List<Vector2> points = new List<Vector2>(count + 1);
            for (int index = 0; index < count; index++)
            {
                points.Add(RadarPoint(center, radius, count, index, 1f));
            }

            points.Add(points[0]);
            return points;
        }

        /// <summary>构建一条闭合数据序列。</summary>
        private static List<Vector2> BuildSeriesPoints(
            Vector2 center,
            float radius,
            IReadOnlyList<RadarChartAxis> axes,
            IReadOnlyList<float> values)
        {
            List<Vector2> points = new List<Vector2>(axes.Count + 1);
            for (int index = 0; index < axes.Count; index++)
            {
                RadarChartAxis axis = axes[index];
                points.Add(RadarPoint(center, radius, axes.Count, index, GetAxisRatio(axis, values[index])));
            }

            points.Add(points[0]);
            return points;
        }

        /// <summary>计算雷达图一点；不夹断超限比例，交给裁切边界处理。</summary>
        private static Vector2 RadarPoint(Vector2 center, float radius, int count, int index, float ratio)
        {
            float angle = -Mathf.PI * 0.5f + Mathf.PI * 2f * index / count;
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * ratio;
        }

        /// <summary>计算两个矩形的交集。</summary>
        private static Rect Intersect(Rect first, Rect second)
        {
            float xMin = Mathf.Max(first.xMin, second.xMin);
            float yMin = Mathf.Max(first.yMin, second.yMin);
            float xMax = Mathf.Min(first.xMax, second.xMax);
            float yMax = Mathf.Min(first.yMax, second.yMax);
            return new Rect(xMin, yMin, Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        }
    }
}
