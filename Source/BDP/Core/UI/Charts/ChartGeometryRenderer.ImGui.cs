#if BDP_RIMWORLD_1_7
using System.Collections.Generic;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using UnityEngine;
using Verse;
using Color = UnityEngine.Color;

namespace BDP.Core.UI.Charts
{
    /// <summary>1.7 屏幕界面几何直接进入原版背景列表，共享窗口绘制顺序与父裁切。</summary>
    internal static partial class ChartGeometryRenderer
    {
        /// <summary>直接描绘一条线，不生成旧材质或手工三角形。</summary>
        internal static void DrawLine(Rect clipRect, Vector2 from, Vector2 to, Color color, float width)
        {
            ImDrawListPtr list;
            if (width <= 0f || !Begin(clipRect, color, out list)) return;
            try { list.AddLine(ToScreen(from), ToScreen(to), ToColor(color), width * Prefs.UIScale); }
            finally { list.PopClipRect(); }
        }

        /// <summary>绘制不带凸边界的实线或虚线折线。</summary>
        internal static void DrawPolyline(Rect clipRect, IReadOnlyList<Vector2> points, Color color,
            float width, bool dashed, float dashLength = DashLength, float dashGap = DashGap)
        {
            DrawPolyline(clipRect, points, color, width, dashed, dashLength, dashGap, null);
        }

        /// <summary>普通折线交给原生描边；雷达凸边界仍精确裁切完整线宽。</summary>
        internal static void DrawPolyline(Rect clipRect, IReadOnlyList<Vector2> points, Color color,
            float width, bool dashed, float dashLength, float dashGap, IReadOnlyList<Vector2> clipPolygon)
        {
            ImDrawListPtr list;
            if (points == null || points.Count < 2 || width <= 0f || !Begin(clipRect, color, out list)) return;
            ImDrawListFlags flags = list.Flags;
            try
            {
                if (clipPolygon != null) list.Flags &= ~ImDrawListFlags.AntiAliasedFill;
                if (!dashed && clipPolygon == null)
                {
                    list.AddPolyline(ToScreen(points), ToColor(color), ImDrawFlags.None, width * Prefs.UIScale);
                    return;
                }
                for (int index = 0; index < points.Count - 1; index++)
                {
                    Vector2 from = points[index], to = points[index + 1];
                    if (!dashed) { DrawSegment(list, from, to, color, width, clipPolygon); continue; }
                    Vector2 delta = to - from;
                    float length = delta.magnitude;
                    if (length <= 0.01f) continue;
                    Vector2 direction = delta / length;
                    float dash = Mathf.Max(0.1f, dashLength), gap = Mathf.Max(0f, dashGap);
                    for (float start = 0f; start < length; start += dash + gap)
                        DrawSegment(list, from + direction * start,
                            from + direction * Mathf.Min(start + dash, length), color, width, clipPolygon);
                }
            }
            finally { list.Flags = flags; list.PopClipRect(); }
        }

        /// <summary>保留显式中心点填充语义，供已有扩展调用。</summary>
        internal static void DrawFilledPolygon(Rect clipRect, Vector2 center, IReadOnlyList<Vector2> points, Color color)
        {
            DrawFilledPolygon(clipRect, center, points, color, null);
        }

        /// <summary>按扇心处理雷达超限与凹轮廓，原生列表负责各凸片填充和矩形裁切。</summary>
        internal static void DrawFilledPolygon(Rect clipRect, Vector2 center, IReadOnlyList<Vector2> points,
            Color color, IReadOnlyList<Vector2> clipPolygon)
        {
            ImDrawListPtr list;
            if (points == null || points.Count < 3 || !Begin(clipRect, color, out list)) return;
            ImDrawListFlags flags = list.Flags;
            try
            {
                // 扇片内部边缘不能重复做透明抗锯齿，否则半透明雷达会出现接缝和叠色。
                list.Flags &= ~ImDrawListFlags.AntiAliasedFill;
                for (int index = 0; index < points.Count; index++)
                    FillConvex(list, ClipToBoundary(new[] { center, points[index], points[(index + 1) % points.Count] },
                        clipPolygon), color);
            }
            finally { list.Flags = flags; list.PopClipRect(); }
        }

        /// <summary>原生圆点填充负责细分与轮廓，不再固定提交十六片三角形。</summary>
        internal static void DrawCircle(Rect clipRect, Vector2 center, float radius, Color color)
        {
            ImDrawListPtr list;
            if (radius <= 0f || !Begin(clipRect, color, out list)) return;
            try { list.AddCircleFilled(ToScreen(center), radius * Prefs.UIScale, ToColor(color)); }
            finally { list.PopClipRect(); }
        }

        /// <summary>原生圆轮廓收回业务页的手工分段循环。</summary>
        internal static void DrawCircleOutline(Rect clipRect, Vector2 center, float radius, Color color, float width, int segments)
        {
            ImDrawListPtr list;
            if (radius <= 0f || width <= 0f || !Begin(clipRect, color, out list)) return;
            try { list.AddCircle(ToScreen(center), radius * Prefs.UIScale, ToColor(color), segments, width * Prefs.UIScale); }
            finally { list.PopClipRect(); }
        }

        /// <summary>圆弧由原生路径采样；虚线只保留连续弧长的实段划分。</summary>
        internal static void DrawArc(Rect clipRect, Vector2 center, float radius, float fromDegrees,
            float toDegrees, Color color, float width, bool dashed, float dashLength, float dashGap)
        {
            float sweep = toDegrees - fromDegrees;
            float length = Mathf.Abs(sweep) * Mathf.Deg2Rad * radius;
            ImDrawListPtr list;
            if (radius <= 0f || length <= 0.01f || width <= 0f || !Begin(clipRect, color, out list)) return;
            try
            {
                if (!dashed) { StrokeArc(list, center, radius, fromDegrees, toDegrees, color, width); return; }
                float dash = Mathf.Max(0.5f, dashLength), gap = Mathf.Max(0f, dashGap);
                for (float start = 0f; start < length; start += dash + gap)
                    StrokeArc(list, center, radius, fromDegrees + sweep * start / length,
                        fromDegrees + sweep * Mathf.Min(start + dash, length) / length, color, width);
            }
            finally { list.PopClipRect(); }
        }

        /// <summary>完整扇区包含中心点；大于半圆使用原生凹路径，整圆直接填圆。</summary>
        internal static void DrawSector(Rect clipRect, Vector2 center, float radius, float fromDegrees,
            float toDegrees, Color color)
        {
            float sweep = toDegrees - fromDegrees;
            ImDrawListPtr list;
            if (radius <= 0f || sweep <= 0f || !Begin(clipRect, color, out list)) return;
            try
            {
                if (sweep >= 360f) { list.AddCircleFilled(ToScreen(center), radius * Prefs.UIScale, ToColor(color)); return; }
                list.PathClear();
                list.PathLineTo(ToScreen(center));
                list.PathArcTo(ToScreen(center), radius * Prefs.UIScale,
                    fromDegrees * Mathf.Deg2Rad, toDegrees * Mathf.Deg2Rad);
                list.PathFillConcave(ToColor(color));
            }
            finally { list.PopClipRect(); }
        }

        /// <summary>原版屏幕 GUI 使用背景列表；不创建窗口、上下文、额外帧或输入桥。</summary>
        private static bool Begin(Rect clipRect, Color color, out ImDrawListPtr list)
        {
            list = default(ImDrawListPtr);
            if (Event.current == null || Event.current.type != EventType.Repaint || color.a <= 0f
                || clipRect.width <= 0f || clipRect.height <= 0f || ImGui.GetCurrentContext().IsNull()) return false;
            list = ImGui.GetBackgroundDrawList();
            // 同一列表已有原版窗口与滚动父裁切。必须求交，不能画到前景或绕过父区域。
            list.PushClipRect(ToScreen(clipRect.min), ToScreen(clipRect.max), true);
            return true;
        }

        /// <summary>只在雷达边界需要时扩出完整线宽；普通线段直接使用原生描边。</summary>
        private static void DrawSegment(ImDrawListPtr list, Vector2 from, Vector2 to, Color color,
            float width, IReadOnlyList<Vector2> boundary)
        {
            if (boundary == null) { list.AddLine(ToScreen(from), ToScreen(to), ToColor(color), width * Prefs.UIScale); return; }
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length <= 0.01f) return;
            Vector2 normal = new Vector2(-delta.y, delta.x) / length * (width * .5f);
            FillConvex(list, ClipToBoundary(new[] { from + normal, to + normal, to - normal, from - normal }, boundary), color);
        }

        /// <summary>仅保留原生矩形裁切不能代替的凸多边形边界数学。</summary>
        private static List<Vector2> ClipToBoundary(IReadOnlyList<Vector2> polygon, IReadOnlyList<Vector2> boundary)
        {
            List<Vector2> result = new List<Vector2>(polygon);
            if (boundary == null || boundary.Count < 3) return result;
            int count = boundary.Count;
            if ((boundary[0] - boundary[count - 1]).sqrMagnitude <= .0001f) count--;
            float area = SignedArea(boundary, count);
            if (count < 3 || Mathf.Abs(area) <= .0001f) return result;
            for (int edge = 0; edge < count && result.Count > 0; edge++)
                result = ClipAgainstConvexEdge(result, boundary[edge], boundary[(edge + 1) % count], area > 0f);
            return result;
        }

        /// <summary>裁切后的扇片或线宽轮廓始终为凸形，按屏幕顺时针方向交给原生填充。</summary>
        private static void FillConvex(ImDrawListPtr list, List<Vector2> points, Color color)
        {
            if (points.Count < 3) return;
            float area = SignedArea(points);
            if (Mathf.Abs(area) <= .0001f) return;
            if (area < 0f) points.Reverse();
            list.AddConvexPolyFilled(ToScreen(points), ToColor(color));
        }

        /// <summary>描绘一节实线弧，细分由游戏原生库负责。</summary>
        private static void StrokeArc(ImDrawListPtr list, Vector2 center, float radius, float fromDegrees,
            float toDegrees, Color color, float width)
        {
            list.PathClear();
            list.PathArcTo(ToScreen(center), radius * Prefs.UIScale,
                fromDegrees * Mathf.Deg2Rad, toDegrees * Mathf.Deg2Rad);
            list.PathStroke(ToColor(color), ImDrawFlags.None, width * Prefs.UIScale);
        }

        /// <summary>原版局部坐标转物理像素，只应用一次界面缩放，原生纵轴直接向下。</summary>
        private static float2 ToScreen(Vector2 point)
        {
            Vector2 screen = Verse.UI.GUIToScreenPoint(point) * Prefs.UIScale;
            return new float2(screen.x, screen.y);
        }

        /// <summary>批量转换同一轮廓的点。</summary>
        private static float2[] ToScreen(IReadOnlyList<Vector2> points)
        {
            float2[] result = new float2[points.Count];
            for (int index = 0; index < points.Count; index++) result[index] = ToScreen(points[index]);
            return result;
        }

        /// <summary>沿用游戏颜色的字节转换，保留实际透明度。</summary>
        private static ImColor8 ToColor(Color color)
        {
            Color32 bytes = color;
            return new ImColor8(bytes.r, bytes.g, bytes.b, bytes.a);
        }
    }
}
#endif
