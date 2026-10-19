using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.UI.Charts
{
    /// <summary>图表几何入口；旧绘法仅用于 1.6，凸边界数学由两版本共用。</summary>
    internal static partial class ChartGeometryRenderer
    {
        /// <summary>虚线中每段实线长度。</summary>
        private const float DashLength = 5f;

        /// <summary>虚线中每段空隙长度。</summary>
        private const float DashGap = 3f;

#if BDP_RIMWORLD_1_6
        // 1.7 不编译旧材质、矩阵、三角形提交和矩形软件裁切；原生实现见同名分部文件。
        /// <summary>绘制局部坐标线段。</summary>
        internal static void DrawLine(Rect clipRect, Vector2 from, Vector2 to, Color color, float width)
        {
            DrawPolyline(clipRect, new[] { from, to }, color, width, false);
        }

        /// <summary>绘制局部坐标折线。</summary>
        internal static void DrawPolyline(
            Rect clipRect,
            IReadOnlyList<Vector2> points,
            Color color,
            float width,
            bool dashed,
            float dashLength = DashLength,
            float dashGap = DashGap)
        {
            DrawPolyline(clipRect, points, color, width, dashed, dashLength, dashGap, null);
        }

        /// <summary>绘制限制在矩形和可选凸多边形内的局部坐标折线。</summary>
        internal static void DrawPolyline(
            Rect clipRect,
            IReadOnlyList<Vector2> points,
            Color color,
            float width,
            bool dashed,
            float dashLength,
            float dashGap,
            IReadOnlyList<Vector2> clipPolygon)
        {
            clipRect = NormalizeClipRect(clipRect);
            if (!HasArea(clipRect) || points == null || points.Count < 2 || width <= 0f || !BeginTriangles(color))
            {
                return;
            }

            try
            {
                for (int index = 0; index < points.Count - 1; index++)
                {
                    if (dashed)
                    {
                        DrawDashedSegment(
                            clipRect,
                            points[index],
                            points[index + 1],
                            width,
                            dashLength,
                            dashGap,
                            clipPolygon);
                    }
                    else
                    {
                        SubmitClippedLineQuad(clipRect, points[index], points[index + 1], width, clipPolygon);
                    }
                }
            }
            finally
            {
                EndTriangles();
            }
        }

        /// <summary>绘制以中心点为扇心的局部坐标填充多边形。</summary>
        internal static void DrawFilledPolygon(
            Rect clipRect,
            Vector2 center,
            IReadOnlyList<Vector2> points,
            Color color)
        {
            DrawFilledPolygon(clipRect, center, points, color, null);
        }

        /// <summary>绘制限制在矩形和可选凸多边形内的扇心填充多边形。</summary>
        internal static void DrawFilledPolygon(
            Rect clipRect,
            Vector2 center,
            IReadOnlyList<Vector2> points,
            Color color,
            IReadOnlyList<Vector2> clipPolygon)
        {
            clipRect = NormalizeClipRect(clipRect);
            if (!HasArea(clipRect) || points == null || points.Count < 3 || !BeginTriangles(color))
            {
                return;
            }

            try
            {
                for (int index = 0; index < points.Count; index++)
                {
                    Vector2 next = points[(index + 1) % points.Count];
                    SubmitClippedPolygon(
                        clipRect,
                        new[] { center, points[index], next },
                        clipPolygon);
                }
            }
            finally
            {
                EndTriangles();
            }
        }

        /// <summary>绘制局部坐标圆点。</summary>
        internal static void DrawCircle(Rect clipRect, Vector2 center, float radius, Color color)
        {
            clipRect = NormalizeClipRect(clipRect);
            if (!HasArea(clipRect) || radius <= 0f || !BeginTriangles(color))
            {
                return;
            }

            try
            {
                const int segments = 16;
                for (int index = 0; index < segments; index++)
                {
                    float firstAngle = Mathf.PI * 2f * index / segments;
                    float secondAngle = Mathf.PI * 2f * (index + 1) / segments;
                    SubmitClippedPolygon(
                        clipRect,
                        new[]
                        {
                            center,
                            center + new Vector2(Mathf.Cos(firstAngle), Mathf.Sin(firstAngle)) * radius,
                            center + new Vector2(Mathf.Cos(secondAngle), Mathf.Sin(secondAngle)) * radius
                        });
                }
            }
            finally
            {
                EndTriangles();
            }
        }

        /// <summary>沿用 1.6 的圆弧采样；虚线节奏按整段弧长连续计算。</summary>
        internal static void DrawArc(Rect clipRect, Vector2 center, float radius, float fromDegrees,
            float toDegrees, Color color, float width, bool dashed, float dashLength, float dashGap)
        {
            float sweep = toDegrees - fromDegrees;
            float arcLength = Mathf.Abs(sweep) * Mathf.Deg2Rad * radius;
            if (radius <= 0f || arcLength <= 0.01f) return;
            if (!dashed)
            {
                DrawPolyline(clipRect, SampleArc(center, radius, fromDegrees, toDegrees), color, width, false);
                return;
            }
            dashLength = Mathf.Max(0.5f, dashLength);
            dashGap = Mathf.Max(0f, dashGap);
            for (float start = 0f; start < arcLength; start += dashLength + dashGap)
            {
                float end = Mathf.Min(start + dashLength, arcLength);
                DrawPolyline(clipRect, SampleArc(center, radius,
                    fromDegrees + sweep * (start / arcLength),
                    fromDegrees + sweep * (end / arcLength)), color, width, false);
            }
        }

        /// <summary>1.6 仍把实线圆弧按约两像素弧长采样，不改变既有绘法。</summary>
        private static List<Vector2> SampleArc(Vector2 center, float radius, float fromDegrees, float toDegrees)
        {
            float length = Mathf.Abs(toDegrees - fromDegrees) * Mathf.Deg2Rad * radius;
            int segments = Mathf.Clamp(Mathf.CeilToInt(length / 2f), 1, 128);
            List<Vector2> points = new List<Vector2>(segments + 1);
            for (int index = 0; index <= segments; index++)
            {
                float radians = Mathf.Lerp(fromDegrees, toDegrees, (float)index / segments) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius);
            }
            return points;
        }

        /// <summary>1.6 圆轮廓保持调用方既有细分数，公共入口只收回重复绘图。</summary>
        internal static void DrawCircleOutline(Rect clipRect, Vector2 center, float radius, Color color,
            float width, int segments)
        {
            segments = segments > 0 ? segments : 36;
            for (int index = 0; index < segments; index++)
            {
                float first = index * Mathf.PI * 2f / segments;
                float next = (index + 1) * Mathf.PI * 2f / segments;
                DrawLine(clipRect, center + new Vector2(Mathf.Cos(first), Mathf.Sin(first)) * radius,
                    center + new Vector2(Mathf.Cos(next), Mathf.Sin(next)) * radius, color, width);
            }
        }

        /// <summary>1.6 扇区保持原有每四度采样和中心点填充，不改成弓形。</summary>
        internal static void DrawSector(Rect clipRect, Vector2 center, float radius, float fromDegrees,
            float toDegrees, Color color)
        {
            float sweep = toDegrees - fromDegrees;
            if (radius <= 0f || sweep <= 0f) return;
            int segments = Mathf.Max(2, Mathf.CeilToInt(sweep / 4f));
            List<Vector2> points = new List<Vector2>(segments + 3) { center };
            for (int index = 0; index <= segments; index++)
            {
                float angle = (fromDegrees + sweep * index / segments) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            points.Add(center);
            DrawFilledPolygon(clipRect, center, points, color);
        }

        /// <summary>开始一组三角形提交。</summary>
        private static bool BeginTriangles(Color color)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return false;
            }

            Material material = SolidColorMaterials.SimpleSolidColorMaterial(color);
            if (material == null || !material.SetPass(0))
            {
                return false;
            }

            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, Screen.width, 0f, Screen.height);
            GL.Begin(GL.TRIANGLES);
            return true;
        }

        /// <summary>结束一组三角形提交。</summary>
        private static void EndTriangles()
        {
            GL.End();
            GL.PopMatrix();
        }

        /// <summary>提交已经裁切的多边形三角扇。</summary>
        private static void SubmitClippedPolygon(
            Rect clipRect,
            IReadOnlyList<Vector2> polygon,
            IReadOnlyList<Vector2> clipPolygon = null)
        {
            List<Vector2> clipped = ClipPolygon(clipRect, polygon, clipPolygon);
            if (clipped.Count < 3)
            {
                return;
            }

            float area = SignedArea(clipped);
            if (Mathf.Abs(area) <= 0.0001f)
            {
                return;
            }

            bool reverse = area < 0f;
            for (int index = 1; index < clipped.Count - 1; index++)
            {
                SubmitVertex(clipped[0]);
                if (reverse)
                {
                    SubmitVertex(clipped[index + 1]);
                    SubmitVertex(clipped[index]);
                }
                else
                {
                    SubmitVertex(clipped[index]);
                    SubmitVertex(clipped[index + 1]);
                }
            }
        }

        /// <summary>提交裁切后的细线四边形。</summary>
        private static void SubmitClippedLineQuad(
            Rect clipRect,
            Vector2 from,
            Vector2 to,
            float width,
            IReadOnlyList<Vector2> clipPolygon = null)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length <= 0.01f)
            {
                return;
            }

            Vector2 normal = new Vector2(-delta.y, delta.x) / length * (width * 0.5f);
            SubmitClippedPolygon(
                clipRect,
                new[]
                {
                    from + normal,
                    to + normal,
                    to - normal,
                    from - normal
                },
                clipPolygon);
        }

        /// <summary>将长线段拆成虚线实线段。</summary>
        private static void DrawDashedSegment(
            Rect clipRect,
            Vector2 from,
            Vector2 to,
            float width,
            float dashLength,
            float dashGap,
            IReadOnlyList<Vector2> clipPolygon)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length <= 0.01f)
            {
                return;
            }

            Vector2 direction = delta / length;
            dashLength = Mathf.Max(0.1f, dashLength);
            dashGap = Mathf.Max(0f, dashGap);
            for (float current = 0f; current < length; current += dashLength + dashGap)
            {
                float next = Mathf.Min(current + dashLength, length);
                SubmitClippedLineQuad(
                    clipRect,
                    from + direction * current,
                    from + direction * next,
                    width,
                    clipPolygon);
            }
        }

        /// <summary>向 OpenGL 提交一个屏幕坐标顶点。</summary>
        private static void SubmitVertex(Vector2 guiPoint)
        {
            Vector2 screenPoint = Verse.UI.GUIToScreenPoint(guiPoint) * Prefs.UIScale;
            screenPoint.y = Screen.height - screenPoint.y;
            GL.Vertex3(screenPoint.x, screenPoint.y, 0f);
        }

        /// <summary>规范化外部传入的裁切矩形。</summary>
        private static Rect NormalizeClipRect(Rect rect)
        {
            return new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, rect.width),
                Mathf.Max(0f, rect.height));
        }

        /// <summary>判断矩形是否有可绘制面积。</summary>
        private static bool HasArea(Rect rect)
        {
            return rect.width > 0f && rect.height > 0f;
        }

        /// <summary>按四条矩形边裁切多边形。</summary>
        private static List<Vector2> ClipPolygon(
            Rect clipRect,
            IReadOnlyList<Vector2> polygon,
            IReadOnlyList<Vector2> clipPolygon = null)
        {
            List<Vector2> result = new List<Vector2>();
            if (polygon == null || polygon.Count == 0)
            {
                return result;
            }

            for (int index = 0; index < polygon.Count; index++)
            {
                result.Add(polygon[index]);
            }

            for (int edge = 0; edge < 4 && result.Count > 0; edge++)
            {
                result = ClipAgainstEdge(result, clipRect, edge);
            }

            if (clipPolygon != null && clipPolygon.Count >= 3 && result.Count > 0)
            {
                int clipCount = clipPolygon.Count;
                if ((clipPolygon[0] - clipPolygon[clipCount - 1]).sqrMagnitude <= 0.0001f)
                {
                    clipCount--;
                }

                float clipArea = SignedArea(clipPolygon, clipCount);
                if (clipCount >= 3 && Mathf.Abs(clipArea) > 0.0001f)
                {
                    for (int edge = 0; edge < clipCount && result.Count > 0; edge++)
                    {
                        Vector2 from = clipPolygon[edge];
                        Vector2 to = clipPolygon[(edge + 1) % clipCount];
                        result = ClipAgainstConvexEdge(result, from, to, clipArea > 0f);
                    }
                }
            }

            return result;
        }

#endif

        /// <summary>按凸多边形的一条有向边裁切多边形。</summary>
        private static List<Vector2> ClipAgainstConvexEdge(
            List<Vector2> polygon,
            Vector2 edgeStart,
            Vector2 edgeEnd,
            bool clipAreaPositive)
        {
            List<Vector2> result = new List<Vector2>();
            Vector2 previous = polygon[polygon.Count - 1];
            float previousSide = Cross(edgeEnd - edgeStart, previous - edgeStart);
            bool previousInside = IsInsideConvexSide(previousSide, clipAreaPositive);
            for (int index = 0; index < polygon.Count; index++)
            {
                Vector2 current = polygon[index];
                float currentSide = Cross(edgeEnd - edgeStart, current - edgeStart);
                bool currentInside = IsInsideConvexSide(currentSide, clipAreaPositive);
                if (currentInside != previousInside)
                {
                    result.Add(IntersectConvexEdge(previous, current, previousSide, currentSide));
                }

                if (currentInside)
                {
                    result.Add(current);
                }

                previous = current;
                previousSide = currentSide;
                previousInside = currentInside;
            }

            return result;
        }

        /// <summary>判断点位于凸多边形某一边的内侧。</summary>
        private static bool IsInsideConvexSide(float side, bool clipAreaPositive)
        {
            return clipAreaPositive ? side >= -0.0001f : side <= 0.0001f;
        }

        /// <summary>计算主体线段与凸多边形边界的交点。</summary>
        private static Vector2 IntersectConvexEdge(
            Vector2 from,
            Vector2 to,
            float fromSide,
            float toSide)
        {
            float denominator = fromSide - toSide;
            if (Mathf.Abs(denominator) <= 0.0001f)
            {
                return from;
            }

            return Vector2.Lerp(from, to, Mathf.Clamp01(fromSide / denominator));
        }

        /// <summary>计算二维向量叉积的标量结果。</summary>
        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

#if BDP_RIMWORLD_1_6
        /// <summary>按指定矩形边裁切多边形。</summary>
        private static List<Vector2> ClipAgainstEdge(List<Vector2> polygon, Rect clipRect, int edge)
        {
            List<Vector2> result = new List<Vector2>();
            Vector2 previous = polygon[polygon.Count - 1];
            bool previousInside = IsInside(previous, clipRect, edge);
            for (int index = 0; index < polygon.Count; index++)
            {
                Vector2 current = polygon[index];
                bool currentInside = IsInside(current, clipRect, edge);
                if (currentInside != previousInside)
                {
                    result.Add(IntersectEdge(previous, current, clipRect, edge));
                }

                if (currentInside)
                {
                    result.Add(current);
                }

                previous = current;
                previousInside = currentInside;
            }

            return result;
        }

        /// <summary>判断点是否在矩形边界内。</summary>
        private static bool IsInside(Vector2 point, Rect clipRect, int edge)
        {
            switch (edge)
            {
                case 0:
                    return point.x >= clipRect.xMin;
                case 1:
                    return point.x <= clipRect.xMax;
                case 2:
                    return point.y >= clipRect.yMin;
                default:
                    return point.y <= clipRect.yMax;
            }
        }

        /// <summary>计算线段与矩形边界的交点。</summary>
        private static Vector2 IntersectEdge(Vector2 from, Vector2 to, Rect clipRect, int edge)
        {
            float denominator;
            float numerator;
            switch (edge)
            {
                case 0:
                    denominator = to.x - from.x;
                    numerator = clipRect.xMin - from.x;
                    break;
                case 1:
                    denominator = to.x - from.x;
                    numerator = clipRect.xMax - from.x;
                    break;
                case 2:
                    denominator = to.y - from.y;
                    numerator = clipRect.yMin - from.y;
                    break;
                default:
                    denominator = to.y - from.y;
                    numerator = clipRect.yMax - from.y;
                    break;
            }

            if (Mathf.Abs(denominator) <= 0.0001f)
            {
                return from;
            }

            return Vector2.Lerp(from, to, Mathf.Clamp01(numerator / denominator));
        }

#endif

        /// <summary>计算多边形有向面积，用于统一顶点绕序。</summary>
        private static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            return SignedArea(polygon, polygon?.Count ?? 0);
        }

        /// <summary>计算指定顶点数量的多边形有向面积。</summary>
        private static float SignedArea(IReadOnlyList<Vector2> polygon, int count)
        {
            float area = 0f;
            for (int index = 0; index < count; index++)
            {
                Vector2 current = polygon[index];
                Vector2 next = polygon[(index + 1) % count];
                area += current.x * next.y - next.x * current.y;
            }

            return area;
        }
    }
}
