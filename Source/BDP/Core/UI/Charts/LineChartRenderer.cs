using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.UI.Charts
{
    /// <summary>中性静态二维折线图渲染器。</summary>
    public static class LineChartRenderer
    {
        /// <summary>绘制一张二维折线图。</summary>
        public static void Draw(
            Rect chartRect,
            Rect visibleRect,
            LineChartData data,
            ChartStyle style = null)
        {
            style = style ?? ChartStyle.CreateDefault();
            if (data == null || !data.XRange.IsValid || !data.YRange.IsValid)
            {
                return;
            }

            Rect drawingRect = new Rect(
                chartRect.x,
                chartRect.y,
                chartRect.width,
                chartRect.height + Mathf.Max(0f, style.LineChartBottomOverflow));
            Rect visible = Intersect(drawingRect, visibleRect.width > 0f && visibleRect.height > 0f ? visibleRect : drawingRect);
            if (visible.width <= 0f || visible.height <= 0f)
            {
                return;
            }

            Widgets.DrawBoxSolidWithOutline(visible, style.BackgroundColor, style.BorderColor);
            ChartCanvas canvas = new ChartCanvas(drawingRect, visible);
            canvas.Draw(context =>
            {
                float left = Mathf.Max(0f, style.PlotLeft);
                float right = Mathf.Max(0f, style.PlotRight);
                float top = Mathf.Max(0f, style.PlotTop);
                float bottom = Mathf.Max(0f, style.PlotBottom);
                Rect plotRect = new Rect(
                    left,
                    top,
                    Mathf.Max(1f, chartRect.width - left - right),
                    Mathf.Max(1f, chartRect.height - top - bottom));
                ChartViewport viewport = new ChartViewport(plotRect, data.XRange, data.YRange);

                DrawGrid(context, viewport, data, style);
                if (data.Series == null)
                {
                    return;
                }

                for (int seriesIndex = 0; seriesIndex < data.Series.Count; seriesIndex++)
                {
                    LineChartSeries series = data.Series[seriesIndex];
                    if (series == null || series.Points == null)
                    {
                        continue;
                    }

                    ChartSeriesStyle seriesStyle = series.Style ?? ChartSeriesStyle.CreateDefault();
                    List<Vector2> segment = new List<Vector2>();
                    for (int pointIndex = 0; pointIndex < series.Points.Count; pointIndex++)
                    {
                        Vector2 mapped;
                        if (viewport.TryMap(series.Points[pointIndex], out mapped))
                        {
                            segment.Add(mapped);
                            if (seriesStyle.ShowValues)
                            {
                                context.DrawText(
                                    new Rect(mapped.x - 28f, mapped.y - 18f, 56f, 16f),
                                    style.FormatValue(series.Points[pointIndex].Y),
                                    style.LabelFont,
                                    seriesStyle.ValueColor,
                                    TextAnchor.MiddleCenter);
                            }
                            continue;
                        }

                        DrawSegment(context, segment, seriesStyle, style);
                        segment.Clear();
                    }

                    DrawSegment(context, segment, seriesStyle, style);
                }
            });
        }

        /// <summary>绘制网格、坐标轴和刻度文字。</summary>
        private static void DrawGrid(
            ChartDrawingContext context,
            ChartViewport viewport,
            LineChartData data,
            ChartStyle style)
        {
            Rect plot = viewport.PlotRect;
            context.DrawLine(new Vector2(plot.x, plot.yMax), new Vector2(plot.xMax, plot.yMax), style.AxisColor, 0.9f);
            context.DrawLine(new Vector2(plot.x, plot.y), new Vector2(plot.x, plot.yMax), style.AxisColor, 0.9f);

            if (data.XTicks != null)
            {
                for (int index = 0; index < data.XTicks.Count; index++)
                {
                    float tick = data.XTicks[index];
                    Vector2 point;
                    if (!viewport.TryMap(new ChartPoint(tick, viewport.YRange.Minimum), out point))
                    {
                        continue;
                    }

                    if (style.ShowXGridLines)
                    {
                        context.DrawLine(
                            new Vector2(point.x, plot.y),
                            new Vector2(point.x, plot.yMax),
                            style.GridColor,
                            0.8f);
                    }

                    context.DrawText(
                        new Rect(
                            point.x - style.XTickLabelWidth * 0.5f,
                            plot.yMax + 2f,
                            style.XTickLabelWidth,
                            style.XTickLabelHeight),
                        style.FormatXValue(tick),
                        style.LabelFont,
                        style.LabelColor,
                        TextAnchor.MiddleCenter);
                }
            }

            if (data.YTicks != null)
            {
                for (int index = 0; index < data.YTicks.Count; index++)
                {
                    float tick = data.YTicks[index];
                    Vector2 point;
                    if (!viewport.TryMap(new ChartPoint(viewport.XRange.Minimum, tick), out point))
                    {
                        continue;
                    }

                    context.DrawLine(
                        new Vector2(plot.x, point.y),
                        new Vector2(plot.xMax, point.y),
                        style.GridColor,
                        0.8f);
                    context.DrawText(
                        new Rect(
                            0f,
                            point.y - style.YTickLabelHeight * 0.5f,
                            style.YTickLabelWidth,
                            style.YTickLabelHeight),
                        style.FormatValue(tick),
                        style.LabelFont,
                        style.LabelColor,
                        TextAnchor.MiddleRight);
                }
            }
        }

        /// <summary>绘制一段连续合法点。</summary>
        private static void DrawSegment(
            ChartDrawingContext context,
            IReadOnlyList<Vector2> points,
            ChartSeriesStyle seriesStyle,
            ChartStyle style)
        {
            if (points == null || points.Count == 0)
            {
                return;
            }

            if (points.Count >= 2)
            {
                context.DrawPolyline(points, seriesStyle);
            }

            if (!seriesStyle.ShowPoints)
            {
                return;
            }

            for (int index = 0; index < points.Count; index++)
            {
                context.DrawCircle(points[index], seriesStyle.PointRadius, seriesStyle.PointColor);
            }
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
