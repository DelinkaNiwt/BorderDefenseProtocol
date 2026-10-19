using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.UI.Charts
{
    /// <summary>公开给扩展者的安全图表绘图上下文。</summary>
    public sealed class ChartDrawingContext
    {
        /// <summary>当前画布。</summary>
        private readonly ChartCanvas canvas;

        /// <summary>只由画布创建绘图上下文。</summary>
        internal ChartDrawingContext(ChartCanvas owner)
        {
            canvas = owner;
        }

        /// <summary>绘制一条细线。</summary>
        public void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1f)
        {
            ChartGeometryRenderer.DrawLine(canvas.ClipRect, ToAbsolute(from), ToAbsolute(to), color, width);
        }

        /// <summary>绘制实线或虚线折线。</summary>
        public void DrawPolyline(IReadOnlyList<Vector2> points, Color color, float width = 1f, bool dashed = false)
        {
            ChartGeometryRenderer.DrawPolyline(canvas.ClipRect, ToAbsolute(points), color, width, dashed);
        }

        /// <summary>按序列样式绘制实线或虚线折线。</summary>
        public void DrawPolyline(IReadOnlyList<Vector2> points, ChartSeriesStyle style)
        {
            style = style ?? ChartSeriesStyle.CreateDefault();
            ChartGeometryRenderer.DrawPolyline(
                canvas.ClipRect,
                ToAbsolute(points),
                style.LineColor,
                style.LineWidth,
                style.Dashed,
                style.DashLength,
                style.DashGap);
        }

        /// <summary>按序列样式绘制并裁切到可选的凸多边形边界。</summary>
        public void DrawPolyline(
            IReadOnlyList<Vector2> points,
            ChartSeriesStyle style,
            IReadOnlyList<Vector2> clipPolygon)
        {
            style = style ?? ChartSeriesStyle.CreateDefault();
            ChartGeometryRenderer.DrawPolyline(
                canvas.ClipRect,
                ToAbsolute(points),
                style.LineColor,
                style.LineWidth,
                style.Dashed,
                style.DashLength,
                style.DashGap,
                ToAbsolute(clipPolygon));
        }

        /// <summary>绘制填充多边形。</summary>
        public void DrawFilledPolygon(IReadOnlyList<Vector2> points, Color color)
        {
            if (points == null || points.Count < 3)
            {
                return;
            }

            Vector2 center = Vector2.zero;
            for (int index = 0; index < points.Count; index++)
            {
                center += points[index];
            }

            center /= points.Count;
            ChartGeometryRenderer.DrawFilledPolygon(
                canvas.ClipRect,
                ToAbsolute(center),
                ToAbsolute(points),
                color);
        }

        /// <summary>绘制填充多边形并裁切到可选的凸多边形边界。</summary>
        public void DrawFilledPolygon(
            IReadOnlyList<Vector2> points,
            Color color,
            IReadOnlyList<Vector2> clipPolygon)
        {
            if (points == null || points.Count < 3)
            {
                return;
            }

            Vector2 center = CalculateCenter(points);
            DrawFilledPolygon(center, points, color, clipPolygon);
        }

        /// <summary>以显式扇心绘制填充多边形，并裁切到可选凸多边形边界。</summary>
        public void DrawFilledPolygon(
            Vector2 center,
            IReadOnlyList<Vector2> points,
            Color color,
            IReadOnlyList<Vector2> clipPolygon)
        {
            if (points == null || points.Count < 3)
            {
                return;
            }

            ChartGeometryRenderer.DrawFilledPolygon(
                canvas.ClipRect,
                ToAbsolute(center),
                ToAbsolute(points),
                color,
                ToAbsolute(clipPolygon));
        }

        /// <summary>
        /// 绘制一段圆弧。角度以度为单位，0 度指向右侧，正方向在 GUI 坐标中为顺时针（向下）。
        /// 虚线按弧长连续切分，避免采样短段各自重启虚线节奏而退化成实线。
        /// </summary>
        public void DrawArc(
            Vector2 center,
            float radius,
            float fromDegrees,
            float toDegrees,
            Color color,
            float width = 1f,
            bool dashed = false,
            float dashLength = 2.5f,
            float dashGap = 2f)
        {
            ChartGeometryRenderer.DrawArc(canvas.ClipRect, ToAbsolute(center), radius,
                fromDegrees, toDegrees, color, width, dashed, dashLength, dashGap);
        }

        /// <summary>绘制圆轮廓；细分数用于保持已有示意图外观，零表示由绘制实现选择。</summary>
        public void DrawCircleOutline(Vector2 center, float radius, Color color, float width = 1f, int segments = 0)
        {
            ChartGeometryRenderer.DrawCircleOutline(canvas.ClipRect, ToAbsolute(center), radius, color, width, segments);
        }

        /// <summary>绘制包含中心点的扇区；角度约定与圆弧一致，零度向右，正向朝下。</summary>
        public void DrawSector(Vector2 center, float radius, float fromDegrees, float toDegrees, Color color)
        {
            ChartGeometryRenderer.DrawSector(canvas.ClipRect, ToAbsolute(center), radius, fromDegrees, toDegrees, color);
        }

        /// <summary>计算给定多边形的顶点平均位置。</summary>
        private static Vector2 CalculateCenter(IReadOnlyList<Vector2> points)
        {
            Vector2 center = Vector2.zero;
            for (int index = 0; index < points.Count; index++)
            {
                center += points[index];
            }

            return center / points.Count;
        }

        /// <summary>绘制一个圆点。</summary>
        public void DrawCircle(Vector2 center, float radius, Color color)
        {
            ChartGeometryRenderer.DrawCircle(canvas.ClipRect, ToAbsolute(center), radius, color);
        }

        /// <summary>绘制经过裁切的文字。</summary>
        public void DrawText(Rect localRect, string text, GameFont font, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            Rect absoluteRect = ToAbsolute(localRect);
            if (absoluteRect.width <= 0f || absoluteRect.height <= 0f)
            {
                return;
            }

            GUI.BeginGroup(canvas.ClipRect);
            try
            {
                GameFont oldFont = Text.Font;
                TextAnchor oldAnchor = Text.Anchor;
                Color oldColor = GUI.color;
                try
                {
                    Text.Font = font;
                    Text.Anchor = anchor;
                    GUI.color = color;
                    Widgets.Label(
                        new Rect(
                            absoluteRect.x - canvas.ClipRect.x,
                            absoluteRect.y - canvas.ClipRect.y,
                            absoluteRect.width,
                            absoluteRect.height),
                        text ?? string.Empty);
                }
                finally
                {
                    GUI.color = oldColor;
                    Text.Anchor = oldAnchor;
                    Text.Font = oldFont;
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        /// <summary>把画布局部坐标转换为外部 GUI 坐标。</summary>
        private Vector2 ToAbsolute(Vector2 point)
        {
            return new Vector2(canvas.Bounds.x + point.x, canvas.Bounds.y + point.y);
        }

        /// <summary>把画布局部矩形转换为外部 GUI 矩形。</summary>
        private Rect ToAbsolute(Rect rect)
        {
            return new Rect(canvas.Bounds.x + rect.x, canvas.Bounds.y + rect.y, rect.width, rect.height);
        }

        /// <summary>把画布局部点集合转换为外部 GUI 点集合。</summary>
        private IReadOnlyList<Vector2> ToAbsolute(IReadOnlyList<Vector2> points)
        {
            if (points == null)
            {
                return null;
            }

            List<Vector2> result = new List<Vector2>(points.Count);
            for (int index = 0; index < points.Count; index++)
            {
                result.Add(ToAbsolute(points[index]));
            }

            return result;
        }
    }
}
